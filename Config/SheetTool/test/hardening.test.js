const test=require('node:test'),assert=require('node:assert/strict'),fs=require('fs'),os=require('os'),path=require('path');
const {buildWorkbook}=require('../lib/schema');
const {exportArtifacts}=require('../lib/exporter');
const config={namespace:'cfg',arraySeparator:',',tables:[{tableName:'Item',rowName:'ItemInfo',idField:'id',indexes:[]}]};
const book=(headers,types,row)=>buildWorkbook(config,{Item:[headers,types,headers.map(()=>''),row]});
test('rejects generated property collisions and invalid C# identifiers',()=>{
 assert.throws(()=>book(['id','foo_bar','foo-bar'],['int','int','int'],[1,2,3]),/Duplicate generated property/);
 assert.throws(()=>book(['id','1value'],['int','int'],[1,2]),/identifier/);
});
test('rejects int32 and float32 overflow before any export',()=>{
 assert.throws(()=>book(['id'],['int'],[2147483648]));
 assert.throws(()=>book(['id','value'],['int','float'],[1,1e39]));
});
test('export rollback preserves both outputs and Unity metadata',()=>{
 const root=fs.mkdtempSync(path.join(os.tmpdir(),'feather-export-'));
 const settings={...config,outputCodeDir:path.join(root,'code'),outputDataDir:path.join(root,'data')};
 const rename=fs.renameSync;
 try {
  const original=book(['id'],['int'],[1]); exportArtifacts(settings,original,'json');
  fs.writeFileSync(path.join(settings.outputCodeDir,'Item.cs.meta'),'guid: preserved');
  const before=fs.readFileSync(path.join(settings.outputDataDir,'Item.json'),'utf8');
  let injected=false;
  fs.renameSync=(source,destination)=>{
   if (!injected && destination===settings.outputDataDir && path.basename(source).startsWith('.feather-stage-')) {injected=true;throw new Error('injected commit failure');}
   return rename(source,destination);
  };
  assert.throws(()=>exportArtifacts(settings,book(['id'],['int'],[2]),'json'),/injected/);
  fs.renameSync=rename;
  assert.equal(fs.readFileSync(path.join(settings.outputDataDir,'Item.json'),'utf8'),before);
  assert.equal(fs.readFileSync(path.join(settings.outputCodeDir,'Item.cs.meta'),'utf8'),'guid: preserved');
  fs.writeFileSync(path.join(settings.outputDataDir,'Item.json.meta'),'guid: data-preserved');
  exportArtifacts(settings,original,'bin');
  assert.equal(fs.readFileSync(path.join(settings.outputDataDir,'Item.bytes.meta'),'utf8'),'guid: data-preserved');
  const bytes=fs.readFileSync(path.join(settings.outputDataDir,'Item.bytes'));
  assert.equal(bytes.readInt32LE(4),-1);
  assert.equal(bytes.readInt32LE(8),1);
  assert.equal(fs.existsSync(path.join(settings.outputDataDir,'Item.json')),false);
 } finally {fs.renameSync=rename;if (path.dirname(root)!==os.tmpdir()) throw new Error('Unsafe fixture');fs.rmSync(root,{recursive:true,force:true});}
});
