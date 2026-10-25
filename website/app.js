(() => {
  const button = document.getElementById('langToggle');
  let language = document.documentElement.lang || 'ar';
  const paint = () => {
    document.documentElement.lang = language;
    document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr';
    document.querySelectorAll('[data-ar][data-en]').forEach((node) => {
      node.textContent = node.dataset[language];
    });
    button.textContent = language === 'ar' ? 'EN' : 'ع';
    button.setAttribute('aria-label', language === 'ar' ? 'Switch to English' : 'التبديل إلى العربية');
  };
  button.addEventListener('click', () => {
    language = language === 'ar' ? 'en' : 'ar';
    paint();
  });
  paint();
})();