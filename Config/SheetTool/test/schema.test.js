const test = require("node:test");
const assert = require("node:assert/strict");
const { buildWorkbook } = require("../lib/schema");

function createConfig(tables) {
  return {
    arraySeparator: ",",
    tables: tables.map((table) => ({
      sheetName: "",
      rowName: `${table.tableName}Info`,
      idField: "id",
      indexes: [],
      ...table
    }))
  };
}

test("builds primitive and enum fields", () => {
  const workbook = buildWorkbook(createConfig([{ tableName: "Item" }]), {
    __enums__: [{
      fileName: "Enums_Gameplay.xlsx",
      rows: [
        ["EnumName", "Name", "Value", "Comment"],
        ["ItemType", "Potion", "1", ""]
      ]
    }],
    Item: [
      ["id", "name", "type"],
      ["int", "string", "enum:ItemType"],
      ["ID", "Name", "Type"],
      [1001, "Potion", "Potion"]
    ]
  });

  assert.equal(workbook.tables[0].rows[0].id, 1001);
  assert.equal(workbook.tables[0].rows[0].type, 1);
});

test("rejects duplicate primary keys", () => {
  assert.throws(() => buildWorkbook(createConfig([{ tableName: "Item" }]), {
    __enums__: [],
    Item: [
      ["id", "name"],
      ["int", "string"],
      ["ID", "Name"],
      [1, "First"],
      [1, "Second"]
    ]
  }), /duplicate value/);
});

test("rejects missing cross-table references", () => {
  const config = createConfig([{ tableName: "Item" }, { tableName: "Monster" }]);
  assert.throws(() => buildWorkbook(config, {
    __enums__: [],
    Item: [
      ["id"],
      ["int"],
      ["ID"],
      [1001]
    ],
    Monster: [
      ["id", "dropItemId"],
      ["int", "ref:Item"],
      ["ID", "Drop"],
      [1, 9999]
    ]
  }), /references missing Item key/);
});

test("rejects an enum split across files", () => {
  assert.throws(() => buildWorkbook(createConfig([{ tableName: "Item" }]), {
    __enums__: [
      {
        fileName: "Enums_A.xlsx",
        rows: [
          ["EnumName", "Name", "Value", "Comment"],
          ["ItemType", "Potion", "1", ""]
        ]
      },
      {
        fileName: "Enums_B.xlsx",
        rows: [
          ["EnumName", "Name", "Value", "Comment"],
          ["ItemType", "Weapon", "2", ""]
        ]
      }
    ],
    Item: [
      ["id"],
      ["int"],
      ["ID"],
      [1]
    ]
  }), /defined in multiple files/);
});
