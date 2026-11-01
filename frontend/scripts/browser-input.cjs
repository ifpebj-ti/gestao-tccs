// Pass form values as CDP arguments so they are never interpreted as JavaScript.
async function fillInput(send, id, value) {
  const context = await send('Runtime.evaluate', { expression: 'globalThis' });
  const objectId = context.result.objectId;
  try {
    const result = await send('Runtime.callFunctionOn', {
      objectId,
      functionDeclaration: `function (id, value) {
        const input = id === null ? document.querySelector('textarea') : document.getElementById(id);
        const prototype = input instanceof HTMLTextAreaElement ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
        Object.getOwnPropertyDescriptor(prototype, 'value').set.call(input, value);
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
      }`,
      arguments: [{ value: id }, { value }],
      returnByValue: true
    });
    if (result.exceptionDetails) throw new Error(result.exceptionDetails.text);
  } finally {
    await send('Runtime.releaseObject', { objectId });
  }
}

module.exports = { fillInput };
