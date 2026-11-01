const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const { fillInput } = require('../scripts/browser-input.cjs');

test('form values remain data and input/change events are dispatched', async () => {
  const payload = "'); globalThis.compromised = true; //\n\u2028\"`";
  const events = [];
  class Input { set value(value) { this.received = value; } }
  const input = new Input();
  input.dispatchEvent = event => events.push(event.type);
  const calls = [];
  await fillInput(async (method, params) => {
    calls.push(method);
    if (method === 'Runtime.evaluate') return { result: { objectId: 'window' } };
    if (method === 'Runtime.callFunctionOn') {
      assert.equal(params.functionDeclaration.includes(payload), false);
      const context = vm.createContext({
        document: { getElementById: id => { assert.equal(id, payload); return input; } },
        HTMLInputElement: Input, HTMLTextAreaElement: class {},
        Event: class { constructor(type) { this.type = type; } }
      });
      vm.runInContext(`(${params.functionDeclaration})`, context)(...params.arguments.map(arg => arg.value));
      assert.equal(context.compromised, undefined);
      return {};
    }
    return {};
  }, payload, payload);
  assert.equal(input.received, payload);
  assert.deepEqual(events, ['input', 'change']);
  assert.equal(calls.at(-1), 'Runtime.releaseObject');
});
