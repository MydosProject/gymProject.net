const test = require('node:test');
const assert = require('node:assert/strict');
const {layout} = require('../src/NO23.Web/wwwroot/js/calendar-timeline.js');
test('overlapping PT/group/consultation occupy separate lanes; adjacent lessons reuse the full column',()=>{
    const result=layout([{id:'PT',start:540,end:600},{id:'Group',start:570,end:620},{id:'Consult',start:610,end:640},{id:'Next',start:640,end:690}]);
    assert.deepEqual(result.map(x=>[x.id,x.lane,x.lanes]),[['PT',0,2],['Group',1,2],['Consult',0,2],['Next',0,1]]);
});
test('three simultaneous lessons have three visible lanes independent of input order',()=>{
    const result=layout([{start:600,end:660},{start:540,end:630},{start:570,end:610}]);
    assert.deepEqual(result.map(x=>x.lane),[0,1,2]); assert.ok(result.every(x=>x.lanes===3));
});
test('empty day and touching lessons do not create overlap',()=>{
    assert.deepEqual(layout([]),[]);
    assert.ok(layout([{start:540,end:600},{start:600,end:650}]).every(x=>x.lane===0&&x.lanes===1));
});
