function buildWorkbook(config, rawSheets) {
  const enums = parseEnums(rawSheets.__enums__ || []);
  const enumMap = new Map(enums.map((entry) => [entry.name, entry]));
  const tables = config.tables.map((tableConfig) =>
    parseTable(config, tableConfig, rawSheets[tableConfig.tableName] || [], enumMap)
  );

  validateAndResolveRefs(tables);

  return {
    enums,
    tables
  };
}

function parseEnums(enumFiles) {
  if (!enumFiles.length) {
    return [];
  }

  const enumMap = new Map();
  for (const enumFile of enumFiles) {
    const rows = enumFile.rows || [];
    if (!rows.length) {
      continue;
    }
    const firstCell = toCell(rows, 0, 0);
    if (firstCell !== "EnumName") {
      throw new Error(`Unsupported enum sheet format in ${enumFile.fileName}. Expected columns: EnumName | Name | Value | Comment.`);
    }
    parseSimpleEnums(enumMap, rows, enumFile.fileName);
  }
  return Array.from(enumMap.values());
}

function parseSimpleEnums(enumMap, rows, fileName) {
  for (let rowIndex = 1; rowIndex < rows.length; rowIndex++) {
    const row = rows[rowIndex] || [];
    if (isRowEmpty(row)) {
      continue;
    }
    const enumName = String(row[0] || "").trim();
    const itemName = String(row[1] || "").trim();
    const itemValue = String(row[2] || "").trim();
    const itemComment = String(row[3] || "").trim();
    if (!enumName || !itemName || !itemValue) {
      throw new Error(`Enum row ${rowIndex + 1} in ${fileName} is missing EnumName, Name or Value.`);
    }
    const enumEntry = getOrCreateEnum(enumMap, enumName, fileName);
    ensureEnumBelongsToSingleFile(enumEntry, enumName, fileName);
    ensureUniqueEnumItem(enumEntry, enumName, itemName, itemValue, fileName, rowIndex + 1);
    enumEntry.items.push({
      name: itemName,
      value: parseInteger(itemValue, `Enum ${enumName} row ${rowIndex + 1} in ${fileName}`),
      comment: itemComment
    });
  }
}

function getOrCreateEnum(enumMap, enumName, fileName) {
  let entry = enumMap.get(enumName);
  if (!entry) {
    entry = { name: enumName, items: [], sourceFile: fileName };
    enumMap.set(enumName, entry);
  }
  return entry;
}

function ensureEnumBelongsToSingleFile(enumEntry, enumName, fileName) {
  if (enumEntry.sourceFile !== fileName) {
    throw new Error(
      `Enum "${enumName}" is defined in multiple files: ${enumEntry.sourceFile} and ${fileName}.`
    );
  }
}

function ensureUniqueEnumItem(enumEntry, enumName, itemName, itemValue, fileName, rowNumber) {
  if (enumEntry.items.some((item) => item.name === itemName)) {
    throw new Error(`Enum "${enumName}" item "${itemName}" is duplicated in ${fileName} row ${rowNumber}.`);
  }
  const parsedValue = parseInteger(itemValue, `Enum ${enumName} row ${rowNumber} in ${fileName}`);
  if (enumEntry.items.some((item) => item.value === parsedValue)) {
    throw new Error(`Enum "${enumName}" value "${itemValue}" is duplicated in ${fileName} row ${rowNumber}.`);
  }
}

function parseTable(config, tableConfig, rows, enumMap) {
  const layout = detectTableLayout(rows);
  const headerNames = layout.headerNames;
  const headerTypes = layout.headerTypes;
  const headerComments = layout.headerComments;
  const rawDataRows = layout.dataRows;

  if (!headerNames.length) {
    throw new Error(`Table ${tableConfig.tableName} has no fields.`);
  }
  if (headerNames.length !== headerTypes.length) {
    throw new Error(`Table ${tableConfig.tableName} field name/type count does not match.`);
  }

  const fields = headerNames.map((fieldName, index) => {
    const originalName = String(fieldName || "").trim();
    if (!originalName) {
      throw new Error(`Table ${tableConfig.tableName} has an empty field name at column ${index + 1}.`);
    }
    return {
      originalName,
      propertyName: toPascalCase(originalName),
      type: parseType(String(headerTypes[index] || "").trim(), enumMap),
      comment: String(headerComments[index] || "").trim()
    };
  });

  const duplicateField = findDuplicate(fields.map((field) => field.originalName.toLowerCase()));
  if (duplicateField) {
    throw new Error(`Table ${tableConfig.tableName} has duplicate field "${duplicateField}".`);
  }

  const idField = resolveField(fields, tableConfig.idField, tableConfig.tableName, "idField");
  const indexes = tableConfig.indexes.map((indexName) =>
    resolveField(fields, indexName, tableConfig.tableName, "index")
  );

  const rowsData = rawDataRows
    .map((row) => parseRow(row, fields, config.arraySeparator, tableConfig.tableName))
    .filter((row) => row !== null);

  validateUniqueKey(tableConfig.tableName, rowsData, idField.originalName);
  for (const indexField of indexes) {
    validateUniqueKey(tableConfig.tableName, rowsData, indexField.originalName);
  }

  return {
    sheetName: tableConfig.sheetName,
    name: tableConfig.tableName,
    rowName: tableConfig.rowName,
    fields,
    idField,
    indexes,
    rows: rowsData
  };
}

function detectTableLayout(rows) {
  if (!rows.length) {
    throw new Error(`Sheet is empty.`);
  }

  return {
    headerNames: rows[0] || [],
    headerTypes: rows[1] || [],
    headerComments: rows[2] || [],
    dataRows: rows.slice(3).map((row, index) => ({ values: row || [], rowNumber: index + 4 }))
  };
}

function parseType(rawType, enumMap) {
  if (!rawType) {
    throw new Error(`Field type cannot be empty.`);
  }

  let isArray = false;
  let typeText = rawType;
  if (rawType.endsWith("[]")) {
    isArray = true;
    typeText = rawType.slice(0, -2);
  }

  let type;
  if (typeText.startsWith("enum:")) {
    const enumName = typeText.slice(5);
    type = createEnumType(enumName, enumMap);
  } else if (typeText.startsWith("ref:")) {
    type = { kind: "ref", refTable: typeText.slice(4) };
  } else if (["int", "float", "string", "bool"].includes(typeText)) {
    type = { kind: "primitive", primitive: typeText };
  } else if (enumMap.has(typeText)) {
    type = createEnumType(typeText, enumMap);
  } else {
    throw new Error(`Unsupported field type "${rawType}"`);
  }

  return isArray ? { kind: "array", elementType: type } : type;
}

function parseRow(row, fields, arraySeparator, tableName) {
  if (isRowEmpty(row.values)) {
    return null;
  }

  const result = {};
  for (let fieldIndex = 0; fieldIndex < fields.length; fieldIndex++) {
    const field = fields[fieldIndex];
    const rawValue = row.values[fieldIndex];
    result[field.originalName] = parseValue(rawValue, field.type, {
      tableName,
      rowIndex: row.rowNumber,
      fieldName: field.originalName,
      arraySeparator
    });
  }
  return result;
}

function parseValue(rawValue, type, context) {
  if (type.kind === "array") {
    if (rawValue === undefined || rawValue === null || String(rawValue).trim() === "") {
      return [];
    }
    const parts = String(rawValue)
      .split(context.arraySeparator)
      .map((part) => part.trim())
      .filter((part) => part.length > 0);
    return parts.map((part) => parseValue(part, type.elementType, context));
  }

  if (type.kind === "primitive") {
    return parsePrimitiveValue(rawValue, type.primitive, context);
  }

  if (type.kind === "enum") {
    if (rawValue === undefined || rawValue === null || String(rawValue).trim() === "") {
      throw new Error(formatContext(context, `Enum value is required.`));
    }
    return String(rawValue).trim();
  }

  if (type.kind === "ref") {
    if (rawValue === undefined || rawValue === null || String(rawValue).trim() === "") {
      throw new Error(formatContext(context, `Reference value is required.`));
    }
    return rawValue;
  }

  throw new Error(formatContext(context, `Unsupported type kind "${type.kind}"`));
}

function parsePrimitiveValue(rawValue, primitive, context) {
  if (primitive === "string") {
    return rawValue === undefined || rawValue === null ? "" : String(rawValue);
  }

  const text = rawValue === undefined || rawValue === null ? "" : String(rawValue).trim();
  if (!text) {
    throw new Error(formatContext(context, `Value is required for type ${primitive}.`));
  }

  switch (primitive) {
    case "int":
      return parseInteger(text, formatContext(context, `Expected int`));
    case "float":
      return parseFloatNumber(text, formatContext(context, `Expected float`));
    case "bool":
      return parseBoolean(text, formatContext(context, `Expected bool`));
    default:
      throw new Error(formatContext(context, `Unsupported primitive type "${primitive}"`));
  }
}

function validateAndResolveRefs(tables) {
  const tableMap = new Map(tables.map((table) => [table.name, table]));

  for (const table of tables) {
    for (const field of table.fields) {
      resolveFieldType(field.type, tableMap);
    }
  }

  for (const table of tables) {
    for (const row of table.rows) {
      for (const field of table.fields) {
        row[field.originalName] = resolveRowValue(field.type, row[field.originalName], tableMap, {
          tableName: table.name,
          fieldName: field.originalName,
          rowValue: row[table.idField.originalName]
        });
      }
    }
  }
}

function resolveFieldType(type, tableMap) {
  if (type.kind === "array") {
    resolveFieldType(type.elementType, tableMap);
    return;
  }

  if (type.kind === "ref") {
    const targetTable = tableMap.get(type.refTable);
    if (!targetTable) {
      throw new Error(`Reference target table "${type.refTable}" does not exist.`);
    }
    const targetPrimitive = resolveCSharpScalarType(targetTable.idField.type);
    if (!["int", "string"].includes(targetPrimitive.kind || targetPrimitive)) {
      throw new Error(`Reference target table "${type.refTable}" must use int or string primary key.`);
    }
    type.resolvedPrimitive = targetTable.idField.type.kind === "primitive" ? targetTable.idField.type.primitive : "int";
    return;
  }
}

function resolveRowValue(type, value, tableMap, context) {
  if (type.kind === "array") {
    return value.map((item) => resolveRowValue(type.elementType, item, tableMap, context));
  }

  if (type.kind === "enum") {
    return resolveEnumValue(type, value, context);
  }

  if (type.kind === "ref") {
    const targetTable = tableMap.get(type.refTable);
    const key = type.resolvedPrimitive === "string" ? String(value) : parseInteger(String(value), formatRowRef(context));
    const exists = targetTable.rows.some((row) => row[targetTable.idField.originalName] === key);
    if (!exists) {
      throw new Error(`${formatRowRef(context)} references missing ${type.refTable} key "${value}".`);
    }
    return key;
  }

  return value;
}

function resolveEnumValue(type, rawValue, context) {
  const enumValue = String(rawValue).trim();
  const item = type.enumItems.find((entry) => entry.name === enumValue);
  if (!item) {
    throw new Error(`${formatRowRef(context)} has invalid enum value "${enumValue}" for ${type.enumName}.`);
  }
  return item.value;
}

function resolveField(fieldArray, name, tableName, fieldUsage) {
  const field = fieldArray.find((entry) => entry.originalName.toLowerCase() === name.toLowerCase());
  if (!field) {
    throw new Error(`Table ${tableName} is missing ${fieldUsage} field "${name}".`);
  }
  return field;
}

function validateUniqueKey(tableName, rows, fieldName) {
  const seen = new Set();
  for (const row of rows) {
    const key = JSON.stringify(row[fieldName]);
    if (seen.has(key)) {
      throw new Error(`Table ${tableName} has duplicate value "${row[fieldName]}" in field "${fieldName}".`);
    }
    seen.add(key);
  }
}

function findDuplicate(values) {
  const seen = new Set();
  for (const value of values) {
    if (seen.has(value)) {
      return value;
    }
    seen.add(value);
  }
  return null;
}

function toPascalCase(value) {
  const parts = String(value)
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .split(/[^a-zA-Z0-9]+/)
    .filter(Boolean);
  if (!parts.length) {
    throw new Error(`Cannot convert "${value}" to a valid C# identifier.`);
  }
  return parts
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join("");
}

function parseInteger(value, context) {
  const number = Number(value);
  if (!Number.isInteger(number)) {
    throw new Error(`${context}: "${value}"`);
  }
  return number;
}

function parseFloatNumber(value, context) {
  const number = Number(value);
  if (!Number.isFinite(number)) {
    throw new Error(`${context}: "${value}"`);
  }
  return number;
}

function parseBoolean(value, context) {
  const normalized = String(value).trim().toLowerCase();
  if (["true", "1", "yes"].includes(normalized)) {
    return true;
  }
  if (["false", "0", "no"].includes(normalized)) {
    return false;
  }
  throw new Error(`${context}: "${value}"`);
}

function isRowEmpty(row) {
  return !(row || []).some((cell) => String(cell || "").trim() !== "");
}

function toCell(rows, rowIndex, columnIndex) {
  return String((((rows[rowIndex] || [])[columnIndex]) || "")).trim();
}

function formatContext(context, message) {
  return `${context.tableName} row ${context.rowIndex} field ${context.fieldName}: ${message}`;
}

function formatRowRef(context) {
  return `${context.tableName} row id=${context.rowValue} field ${context.fieldName}`;
}

function resolveCSharpScalarType(type) {
  if (type.kind === "primitive") {
    return type.primitive;
  }
  if (type.kind === "enum") {
    return "int";
  }
  throw new Error(`Unsupported scalar type resolution for ${type.kind}`);
}

function createEnumType(enumName, enumMap) {
  const enumEntry = enumMap.get(enumName);
  if (!enumEntry) {
    throw new Error(`Unknown enum type "${enumName}"`);
  }
  return {
    kind: "enum",
    enumName,
    enumItems: enumEntry.items
  };
}

module.exports = {
  buildWorkbook
};
