// Converts the standalone Ocharo Journal content module into the blog seed (topics + stories + asset names).
// Usage: node engineering/scripts/blog/import-journal.mjs <path-to/och-content.mjs>
// Output: engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Api/SeedData/journal-seed.json
import { createHash } from 'node:crypto';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const source = resolve(process.argv[2] ?? '');
if (!process.argv[2]) {
  console.error('Pass the path to och-content.mjs');
  process.exit(1);
}
const { topics, stories } = await import(pathToFileURL(source).href);
const output = fileURLToPath(new URL('../../codebase/ocharo-blog.backend-services/OcharoBlog.Api/SeedData/journal-seed.json', import.meta.url));
const seed = {
  format: 'ocharo-blog-seed/1',
  source: { path: 'ocharo-marketing/engineering/codebase/ocharo-marketing.frontend-services/apps/blog/och-content.mjs', sha256: createHash('sha256').update(await readFile(source)).digest('hex') },
  assets: { logo: 'och-logo.png', hero: 'och-collection-study.png', band: 'och-collection.png' },
  topics: topics.map(({ key, name, short, description }) => ({ key, name, short, description })),
  stories,
};
await mkdir(dirname(output), { recursive: true });
await writeFile(output, JSON.stringify(seed, null, 2) + '\n');
console.log(`Wrote ${stories.length} stories and ${topics.length} topics to ${output}`);
