const fs = require("fs");
const path = require("path");

function loadConfig(configPath) {
  if (!fs.existsSync(configPath)) {
    throw new Error(`Missing config file: ${configPath}`);
  }

  const toolRoot = path.dirname(configPath);
  const raw = JSON.parse(fs.readFileSync(configPath, "utf8"));
  const config = {
    toolRoot,
    excelDir: resolvePath(toolRoot, raw.excelDir || "./Excels"),
    enumFilePattern: String(raw.enumFilePattern || "^Enums([_-].+)?\\.xlsx$").trim(),
    namespace: String(raw.namespace || "cfg").trim(),
    enumSheetName: String(raw.enumSheetName || "").trim(),
    arraySeparator: String(raw.arraySeparator || ","),
    outputCodeDir: resolvePath(toolRoot, raw.outputCodeDir || "../../Client/Assets/Gen"),
    outputDataDir: resolvePath(toolRoot, raw.outputDataDir || "../../Client/Assets/Resources/Config"),
    tableOverrides: normalizeTableOverrides(raw.tableOverrides)
  };

  if (!fs.existsSync(config.excelDir)) {
    throw new Error(`Excel directory was not found: ${config.excelDir}`);
  }
  config.enumFiles = discoverEnumFiles(config);
  if (!config.enumFiles.length) {
    throw new Error(`No enum excel files were found in ${config.excelDir}`);
  }
  config.tables = discoverTables(config);
  if (!config.tables.length) {
    throw new Error(`No excel tables were found in ${config.excelDir}`);
  }

  return config;
}

function normalizeTableOverrides(rawOverrides) {
  if (!rawOverrides || typeof rawOverrides !== "object" || Array.isArray(rawOverrides)) {
    return {};
  }

  const overrides = {};
  for (const [tableName, value] of Object.entries(rawOverrides)) {
    const normalizedTableName = String(tableName || "").trim();
    if (!normalizedTableName) {
      continue;
    }
    overrides[normalizedTableName] = normalizeTableConfig({
      ...(value || {}),
      tableName: normalizedTableName
    });
  }
  return overrides;
}

function discoverTables(config) {
  const enumFileNames = new Set(config.enumFiles.map((fileName) => fileName.toLowerCase()));
  const tableFiles = fs
    .readdirSync(config.excelDir, { withFileTypes: true })
    .filter((entry) => entry.isFile())
    .map((entry) => entry.name)
    .filter((fileName) => isExcelFile(fileName))
    .filter((fileName) => !isExcelTempFile(fileName))
    .filter((fileName) => !enumFileNames.has(fileName.toLowerCase()))
    .sort((left, right) => left.localeCompare(right, "en"));

  const tables = tableFiles.map((fileName) => {
    const tableName = stripExtension(fileName);
    const override = config.tableOverrides[tableName] || {};
    return normalizeTableConfig({
      ...override,
      fileName,
      tableName
    });
  });

  ensureUniqueTableNames(tables);
  return tables;
}

function discoverEnumFiles(config) {
  const pattern = new RegExp(config.enumFilePattern, "i");
  return fs
    .readdirSync(config.excelDir, { withFileTypes: true })
    .filter((entry) => entry.isFile())
    .map((entry) => entry.name)
    .filter((fileName) => isExcelFile(fileName))
    .filter((fileName) => !isExcelTempFile(fileName))
    .filter((fileName) => pattern.test(fileName))
    .sort((left, right) => left.localeCompare(right, "en"));
}

function normalizeTableConfig(table) {
  const tableName = String(table.tableName || stripExtension(table.fileName)).trim();
  if (!tableName) {
    throw new Error(`Table name cannot be empty`);
  }

  return {
    fileName: String(table.fileName || `${tableName}.xlsx`).trim(),
    sheetName: String(table.sheetName || "").trim(),
    tableName,
    rowName: String(table.rowName || `${tableName}Info`).trim(),
    idField: String(table.idField || "id").trim(),
    indexes: Array.isArray(table.indexes) ? table.indexes.map((value) => String(value).trim()).filter(Boolean) : []
  };
}

function stripExtension(fileName) {
  return String(fileName || "").replace(/\.[^.]+$/, "");
}

function isExcelFile(fileName) {
  return /\.xlsx$/i.test(fileName);
}

function isExcelTempFile(fileName) {
  return /^\~\$/.test(fileName);
}

function ensureUniqueTableNames(tables) {
  const seen = new Set();
  for (const table of tables) {
    const key = table.tableName.toLowerCase();
    if (seen.has(key)) {
      throw new Error(`Duplicate table name detected: ${table.tableName}`);
    }
    seen.add(key);
  }
}

function resolvePath(root, target) {
  return path.resolve(root, target);
}

module.exports = {
  loadConfig
};
