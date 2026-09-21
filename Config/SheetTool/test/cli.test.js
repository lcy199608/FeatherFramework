const test = require("node:test");
const assert = require("node:assert/strict");
const { normalizeFormat, resolveExportFormat } = require("../lib/arguments");

test("normalizes supported formats", () => {
  assert.equal(normalizeFormat(" JSON "), "json");
  assert.equal(normalizeFormat("Bin"), "bin");
});

test("rejects unsupported formats", () => {
  assert.throws(() => normalizeFormat("xml"), /Unsupported export format/);
});

test("parses separate and inline format flags", () => {
  assert.equal(resolveExportFormat(["--format", "bin"]), "bin");
  assert.equal(resolveExportFormat(["--format=json"]), "json");
  assert.equal(resolveExportFormat([]), "json");
});
