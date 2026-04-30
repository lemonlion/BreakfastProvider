/**
 * MSW v2 requires Web API globals (Request, Response, etc.) that jsdom doesn't provide.
 * This polyfill file must be loaded via jest `setupFiles` (before setupFilesAfterEnv).
 * @see https://mswjs.io/docs/faq#requestresponsetextencoder-is-not-defined-jest
 */

const { TextDecoder, TextEncoder } = require('node:util');
const { ReadableStream, TransformStream, WritableStream } = require('node:stream/web');

Object.defineProperties(globalThis, {
  TextDecoder: { value: TextDecoder },
  TextEncoder: { value: TextEncoder },
  ReadableStream: { value: ReadableStream },
  TransformStream: { value: TransformStream },
  WritableStream: { value: WritableStream },
});

const { Blob, File } = require('node:buffer');
const { fetch, Headers, FormData, Request, Response } = require('undici');
const { BroadcastChannel } = require('node:worker_threads');

Object.defineProperties(globalThis, {
  fetch: { value: fetch, writable: true, configurable: true },
  Blob: { value: Blob, configurable: true },
  File: { value: File, configurable: true },
  Headers: { value: Headers, configurable: true },
  FormData: { value: FormData, configurable: true },
  Request: { value: Request, configurable: true },
  Response: { value: Response, configurable: true },
  BroadcastChannel: { value: BroadcastChannel, configurable: true },
});

// jsdom doesn't provide matchMedia
Object.defineProperty(globalThis, 'matchMedia', {
  writable: true,
  configurable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});
