const keywords = new Set(('abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while').split(' '));
function identifier(value, context) {
  if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(value) || keywords.has(value)) throw new Error(`${context}: invalid C# identifier '${value}'.`);
}
function validateNames(config, workbook) {
  for (const part of config.namespace.split('.')) identifier(part, 'namespace');
  const names = new Set(['tables','list','dictionary','binaryreader','textasset','resources','jsonconvert','filenotfoundexception','memorystream','invaliddataexception','system']);
  for (const name of [...workbook.enums.map(e=>e.name), ...workbook.tables.flatMap(t=>[t.name,t.rowName])]) {
    identifier(name, 'type');
    if (names.has(name.toLowerCase())) throw new Error(`Duplicate generated type: ${name}`);
    names.add(name.toLowerCase());
  }
  for (const entry of workbook.enums) for (const item of entry.items) { identifier(item.name, entry.name); if (item.name===entry.name) throw new Error('Enum member conflicts with its type'); }
  for (const table of workbook.tables) {
    const members=new Set(['ReadFrom',table.rowName]);
    if (['Load','ReadString','ReadArray','BinaryMagic','ReadCount','LoadRows','IsBinaryConfig','LoadBinaryRows'].includes(table.name)) throw new Error(`Table name conflicts with generated member: ${table.name}`);
    for (const field of table.fields) {
      identifier(field.propertyName, table.name);
      if (members.has(field.propertyName)) throw new Error(`Duplicate generated property: ${table.name}.${field.propertyName}`);
      members.add(field.propertyName);
    }
  }
}
module.exports={identifier,validateNames};
