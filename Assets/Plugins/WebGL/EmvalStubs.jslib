// Заглушки для libAppUITextMateLib.a (пакет App UI из AI Assistant): он собран старым emscripten
// и просит функции, которых в новом embind нет. В игре этот код не вызывается.
mergeInto(LibraryManager.library, {
  _emval_take_value__sig: 'ppp',
  _emval_take_value: function (type, arg) { throw new Error('_emval_take_value stub'); },
  _emval_as__sig: 'dppp',
  _emval_as: function (handle, returnType, destructorsRef) { throw new Error('_emval_as stub'); }
});
