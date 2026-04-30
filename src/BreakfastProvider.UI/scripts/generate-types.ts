import { readFileSync, writeFileSync } from 'fs';
import openapiTS, { astToString } from 'openapi-typescript';
import path from 'path';

async function main() {
  const specPath = path.resolve(__dirname, '../../../docs/openapi.json');
  const outputPath = path.resolve(__dirname, '../src/lib/api/generated-types.ts');

  console.log(`Reading OpenAPI spec from: ${specPath}`);
  const spec = JSON.parse(readFileSync(specPath, 'utf-8'));

  const ast = await openapiTS(spec, {
    immutable: true,
  });

  const output = `// Auto-generated from docs/openapi.json — DO NOT EDIT\n// Regenerate with: pnpm generate:types\n\n${astToString(ast)}`;

  writeFileSync(outputPath, output, 'utf-8');
  console.log(`Generated types written to: ${outputPath}`);
}

main().catch(console.error);
