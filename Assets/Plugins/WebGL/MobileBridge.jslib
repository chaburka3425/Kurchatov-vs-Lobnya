// Телефон в браузере: ввод текста через окно браузера (у IMGUI нет экранной клавиатуры) и проверка «это телефон»
mergeInto(LibraryManager.library, {
  MpkaPrompt: function (titlePtr, defPtr) {
    var def = UTF8ToString(defPtr);
    var r = window.prompt(UTF8ToString(titlePtr), def);
    if (r === null) r = def;
    var size = lengthBytesUTF8(r) + 1;
    var buf = _malloc(size);
    stringToUTF8(r, buf, size);
    return buf;
  },
  // Поделиться/скопировать: на телефоне — меню «Поделиться» (Telegram и т.д.), на ПК — в буфер обмена
  MpkaShare: function (textPtr, share) {
    var t = UTF8ToString(textPtr);
    try {
      if (share && navigator.share) { navigator.share({ title: 'МПКА 2', text: 'Заходи ко мне в МПКА 2!', url: t }).catch(function () {}); return; }
      if (navigator.clipboard && window.isSecureContext) { navigator.clipboard.writeText(t).catch(function () { window.prompt('Скопируй:', t); }); return; }
    } catch (e) {}
    window.prompt('Скопируй:', t);
  },
  MpkaIsMobile: function () {
    return (/Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent) || (navigator.maxTouchPoints > 1 && /Macintosh/.test(navigator.userAgent))) ? 1 : 0;
  }
});
