const fs = require("fs");
const path = require("path");
const XLSX = require("xlsx");

function loadExcelData(config) {
  const rawSheets = {};
  rawSheets.__enums__ = config.enumFiles.map((fileName) => ({
    fileName,
    rows: loadWorksheetRows(path.resolve(config.excelDir, fileName), config.enumSheetName)
  }));

  for (const table of config.tables) {
    rawSheets[table.tableName] = loadWorksheetRows(
      path.resolve(config.excelDir, table.fileName),
      table.sheetName
    );
  }

  return rawSheets;
}

function loadWorksheetRows(filePath, sheetName) {
  if (!fs.existsSync(filePath)) {
    throw new Error(`Excel file was not found: ${filePath}`);
  }

  const workbook = XLSX.readFile(filePath, {
    cellDates: false,
    cellNF: false,
    cellText: false
  });

  const targetSheetName = sheetName || workbook.SheetNames[0];
  if (!targetSheetName) {
    throw new Error(`Excel file has no worksheets: ${filePath}`);
  }

  const worksheet = workbook.Sheets[targetSheetName];
  if (!worksheet) {
    throw new Error(`Worksheet "${targetSheetName}" was not found in ${filePath}`);
  }

  return XLSX.utils.sheet_to_json(worksheet, {
    header: 1,
    raw: false,
    defval: "",
    blankrows: false
  });
}

module.exports = {
  loadExcelData
};
