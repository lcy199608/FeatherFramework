#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const { loadConfig } = require("./lib/config");
const { loadExcelData } = require("./lib/excelFiles");
const { buildWorkbook } = require("./lib/schema");
const { exportArtifacts } = require("./lib/exporter");

async function main() {
  const command = process.argv[2] || "sync";
  if (!["sync", "validate", "export"].includes(command)) {
    throw new Error(`Unsupported command: ${command}`);
  }

  const config = loadConfig(path.resolve(__dirname, "config.json"));
  const format = resolveExportFormat(process.argv.slice(3));
  const rawSheets = loadExcelData(config);
  const workbook = buildWorkbook(config, rawSheets);

  console.log(
    `Validated ${workbook.tables.length} table(s) and ${workbook.enums.length} enum(s) from ${config.excelDir}.`
  );

  if (command === "validate") {
    return;
  }

  exportArtifacts(config, workbook, format);
  console.log(`Export completed. Code => ${config.outputCodeDir}`);
  console.log(`Export completed. Data => ${config.outputDataDir} (${format})`);
}

function resolveExportFormat(args) {
  const formatFlagIndex = args.findIndex((value) => value === "--format");
  if (formatFlagIndex >= 0) {
    const nextValue = args[formatFlagIndex + 1];
    return normalizeFormat(nextValue);
  }

  const inlineFlag = args.find((value) => value.startsWith("--format="));
  if (inlineFlag) {
    return normalizeFormat(inlineFlag.slice("--format=".length));
  }

  return "json";
}

function normalizeFormat(value) {
  const format = String(value || "").trim().toLowerCase();
  if (format === "json" || format === "bin") {
    return format;
  }
  throw new Error(`Unsupported export format: ${value}. Expected json or bin.`);
}

main().catch((error) => {
  const message = error && error.stack ? error.stack : String(error);
  fs.writeFileSync(path.resolve(__dirname, "last-error.log"), `${message}\n`, "utf8");
  console.error(message);
  process.exit(1);
});
