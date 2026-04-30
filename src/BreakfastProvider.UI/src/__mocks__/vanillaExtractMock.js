/**
 * Deep proxy mock for vanilla-extract .css.ts files.
 * Handles: recipe() function calls, nested theme vars, string coercion.
 */
function createDeepProxy() {
  const fn = function () {
    return 'mock-class';
  };
  return new Proxy(fn, {
    get(_target, prop) {
      if (prop === Symbol.toPrimitive) return () => 'mock-css-value';
      if (prop === 'toString' || prop === 'valueOf') return () => 'mock-css-value';
      if (typeof prop === 'symbol') return undefined;
      if (prop === '__esModule') return false;
      return createDeepProxy();
    },
    apply() {
      return 'mock-class';
    },
  });
}

module.exports = new Proxy(
  {},
  {
    get(_target, prop) {
      if (prop === '__esModule') return true;
      if (typeof prop === 'symbol') return undefined;
      return createDeepProxy();
    },
  },
);
