const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("fs");
const os = require("os");
const path = require("path");
const { cleanDirectory } = require("../lib/exporter");

test("cleans generated files without deleting Unity assembly boundaries", () => {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), "feather-exporter-"));
  try {
    fs.writeFileSync(path.join(directory, "Old.cs"), "stale");
    fs.writeFileSync(path.join(directory, "Feature.asmdef"), "{}");
    fs.writeFileSync(path.join(directory, "Feature.asmdef.meta"), "guid: test");

    cleanDirectory(directory, [".meta", ".asmdef", ".asmref"]);

    assert.equal(fs.existsSync(path.join(directory, "Old.cs")), false);
    assert.equal(fs.existsSync(path.join(directory, "Feature.asmdef")), true);
    assert.equal(fs.existsSync(path.join(directory, "Feature.asmdef.meta")), true);
  } finally {
    fs.rmSync(directory, { recursive: true, force: true });
  }
});
