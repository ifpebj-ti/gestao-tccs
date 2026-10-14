const path = require('node:path');

// Exclusivo da validação offline: substitui o download por uma fonte local.
// Não é usado pelo build de produção, nem comprova a aparência da Poppins.
const localFont = path
  .resolve(__dirname, '../public/rawline-cdnfonts/rawline-400.ttf')
  .replaceAll('\\', '/')
  .replace(/^[A-Za-z]:/, '');

module.exports = new Proxy(
  {},
  {
    get(_target, url) {
      if (
        typeof url !== 'string' ||
        !url.startsWith('https://fonts.googleapis.com/')
      )
        return undefined;
      const family = new URL(url).searchParams.get('family').split(':')[0];
      return `@font-face { font-family: '${family}'; font-style: normal; font-weight: 100 900; src: url(${localFont}) format('truetype'); }`;
    }
  }
);
