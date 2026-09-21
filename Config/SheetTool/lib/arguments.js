function resolveExportFormat(args) {
  const formatFlagIndex = args.findIndex((value) => value === "--format");
  if (formatFlagIndex >= 0) {
    return normalizeFormat(args[formatFlagIndex + 1]);
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

module.exports = {
  normalizeFormat,
  resolveExportFormat
};
