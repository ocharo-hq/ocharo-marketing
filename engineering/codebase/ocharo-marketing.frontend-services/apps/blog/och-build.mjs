import { cp, mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { topics, stories } from './och-content.mjs';
const outputDir = fileURLToPath(new URL('./dist/', import.meta.url));
const publicDir = fileURLToPath(new URL('./public/', import.meta.url));
await mkdir(outputDir, { recursive: true });
await cp(publicDir, outputDir, { recursive: true });

const slugs = new Set();
for (const story of stories) {
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(story.slug) || slugs.has(story.slug)) throw new Error(`Invalid or duplicate story address: ${story.slug}`);
  slugs.add(story.slug);
  if (!topics.some(topic => topic.key === story.topic)) throw new Error(`Unknown topic in ${story.slug}`);
  if (!story.title || !story.excerpt || !story.intro || !story.sections.length) throw new Error(`Incomplete story: ${story.slug}`);
  if (story.image && (!story.alt || !story.caption)) throw new Error(`Image needs a description and caption: ${story.slug}`);
  for (const source of story.sources || []) {
    if (!source.label || new URL(source.url).protocol !== 'https:') throw new Error(`Invalid source in ${story.slug}`);
  }
  for (const section of story.sections) {
    for (const index of section.sourceIndexes || []) {
      if (!Number.isInteger(index) || !story.sources?.[index]) throw new Error(`Missing source in ${story.slug}: ${index}`);
    }
  }
}
for (const story of stories) {
  if (story.relatedSlugs && (story.relatedSlugs.length !== 2 || story.relatedSlugs.some(slug => slug === story.slug || !slugs.has(slug)))) throw new Error(`Invalid related stories: ${story.slug}`);
}

const esc = value => String(value).replace(/[&<>"']/g, character => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;' })[character]);
const icon = path => `<svg class="link-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">${path}</svg>`;
const arrow = icon('<path d="M5 12h14M13 6l6 6-6 6"/>');
const externalArrow = icon('<path d="M6 18 18 6M6 6h12v12"/>');
const backArrow = icon('<path d="M19 12H5m6-6-6 6 6 6"/>');
const topicOf = story => topics.find(topic => topic.key === story.topic);
const storyLink = story => `/journal/${story.slug}/`;
const topicLink = topic => `/topics/${topic.key}/`;
const readTime = story => Math.max(1, Math.ceil([
  story.intro, ...story.sections.flatMap(section => [...section.paragraphs, ...(section.list || [])]),
  story.takeaway || '', story.closingQuestion || '',
].join(' ').split(/\s+/).length / 200));

const head = (title, description) => `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${esc(title)} — Ocharo Blog</title><meta name="description" content="${esc(description)}"><meta name="robots" content="noindex,nofollow"><meta name="theme-color" content="#f8f7f3"><link rel="stylesheet" href="/och-style.css"><link rel="icon" type="image/svg+xml" href="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 32 32'%3E%3Crect width='32' height='32' rx='7' fill='%23dd492f'/%3E%3Ctext x='16' y='24' font-family='Georgia' font-size='27' text-anchor='middle' fill='white'%3EO%3C/text%3E%3C/svg%3E"></head><body><a class="skip" href="#main">Skip to content</a>`;
const header = (active = '') => `<header class="site-header"><div class="header-inner"><a class="brand" href="/" aria-label="Ocharo Blog home"><img src="/assets/och-logo.png" alt="Ocharo" width="162" height="60"><span class="brand-divider"></span><span class="brand-blog">Blog</span></a><nav aria-label="Main navigation"><a href="/journal/" ${active === 'all' ? 'aria-current="page"' : ''}>All stories</a>${topics.map(topic => `<a href="${topicLink(topic)}" ${active === topic.key ? 'aria-current="page"' : ''}>${esc(topic.short)}</a>`).join('')}</nav></div></header>`;
const footer = () => `<footer class="site-footer"><div class="footer-inner"><a class="footer-brand" href="/">Ocharo<span>Contemporary Uzbek clothing</span></a><div class="footer-links"><a href="https://www.instagram.com/ocharostile/" target="_blank" rel="noopener noreferrer">Instagram ${externalArrow}</a><a href="https://t.me/Ocharo_Style" target="_blank" rel="noopener noreferrer">Contact on Telegram ${externalArrow}</a></div><p class="edition-note">Preview edition · Editorial drafts</p></div></footer></body></html>`;
const cardVisual = story => story.image
  ? `<img src="/assets/${esc(story.image)}" alt="${esc(story.alt)}" loading="lazy" width="1086" height="1448">`
  : `<span class="cover-format">${esc(story.format || topicOf(story).short)}</span><span class="cover-prompt">${esc(story.cardPrompt || story.title)}</span><span class="cover-blog">Ocharo Blog</span>`;
const card = story => `<article class="story-card"><a class="card-image ${story.image ? esc(story.cardClass || '') : `type-cover ${esc(story.topic)}`}" href="${storyLink(story)}" aria-label="Read: ${esc(story.title)}">${cardVisual(story)}</a><div class="card-copy"><div class="story-meta"><a href="${topicLink(topicOf(story))}">${esc(topicOf(story).short)}</a><span>${readTime(story)} min read</span></div><h3><a href="${storyLink(story)}">${esc(story.title)}</a></h3><p>${esc(story.excerpt)}</p><a class="text-link" href="${storyLink(story)}">Read the story ${arrow}</a></div></article>`;
const homeSelection = () => topics.flatMap(topic => stories.filter(story => story.topic === topic.key).slice(1, 4));
const home = () => {
  const featured = stories[0];
  return `${head('Stories about clothes and the lives we wear them in', 'Wardrobe puzzles, styling experiments, and textile stories from the Ocharo Blog.')}${header()}<main id="main" class="home-main"><section class="masthead"><p class="eyebrow">Clothes. Curiosity. Everyday life.</p><h1>Ocharo Blog<span class="title-dot">.</span></h1><p class="masthead-description">For the clothes you keep thinking about,<br class="desktop-break"> and the questions a photograph cannot answer.</p></section><section class="featured" aria-labelledby="feature-title"><a class="featured-image" href="${storyLink(featured)}" aria-label="Read: ${esc(featured.title)}"><img src="/assets/och-collection-study.png" alt="A collection visualisation of patterned Ocharo nimcha designs on mannequins." width="1122" height="1402" fetchpriority="high"><span class="image-label">Collection visualisation</span></a><div class="featured-copy"><p class="eyebrow"><span class="feature-tag">Featured story</span> ${esc(topicOf(featured).short)}</p><h2 id="feature-title"><a href="${storyLink(featured)}">${esc(featured.title)}</a></h2><p>${esc(featured.excerpt)}</p><a class="filled-link" href="${storyLink(featured)}">Read the story ${arrow}</a><p class="feature-footnote">${readTime(featured)} min read · ${esc(featured.format || 'Essay')}</p></div></section><section class="latest" aria-labelledby="latest-title"><div class="section-heading"><h2 id="latest-title">Something to think about</h2><a class="text-link archive-link" href="/journal/">Browse all ${stories.length} stories ${arrow}</a></div><div class="stories-grid">${homeSelection().map(card).join('')}</div></section><section class="pattern-note" aria-label="A closer look at cloth"><div><p class="eyebrow">Cloth & craft</p><h2>A pattern is<br>only the beginning.</h2><a class="text-link" href="${topicLink(topics.find(topic => topic.key === 'cloth'))}">Follow the thread ${arrow}</a></div><div><img src="/assets/och-collection.png" alt="A visual study bringing different patterned nimcha designs together." loading="lazy" width="3200" height="660"><p class="visual-note">Collection visualisation</p></div></section></main>${footer()}`;
};
const archivePage = () => `${head('All stories', 'Thirty complete stories about everyday clothing, style, and cloth.')}${header('all')}<main id="main" class="topic-main"><a class="back-link" href="/">${backArrow} Blog home</a><section class="topic-heading"><p class="eyebrow">The reading room · ${stories.length} stories</p><h1>All stories<span class="title-dot">.</span></h1><p>A wardrobe dilemma, an experiment to try, a detail you might have missed. Start with a question that feels familiar.</p></section><div class="stories-grid">${stories.map(card).join('')}</div></main>${footer()}`;
const topicPage = topic => `${head(topic.name, topic.description)}${header(topic.key)}<main id="main" class="topic-main"><a class="back-link" href="/journal/">${backArrow} All stories</a><section class="topic-heading"><p class="eyebrow">Ocharo Blog · ${stories.filter(story => story.topic === topic.key).length} stories</p><h1>${esc(topic.name)}<span class="title-dot">.</span></h1><p>${esc(topic.description)}</p></section><div class="stories-grid">${stories.filter(story => story.topic === topic.key).map(card).join('')}</div></main>${footer()}`;

const sectionSources = (story, section) => (section.sourceIndexes || []).length
  ? `<p class="section-sources">Further reading: ${section.sourceIndexes.map(index => { const source = story.sources[index]; return `<a href="${esc(source.url)}" target="_blank" rel="noopener noreferrer">${esc(source.label)} ${externalArrow}</a>`; }).join(' · ')}</p>`
  : '';
const articleCover = story => story.image ? `<figure class="article-cover ${esc(story.cardClass || '')}"><div class="cover-surface"><img src="/assets/${esc(story.image)}" alt="${esc(story.alt)}" width="${story.cardClass === 'collection' ? 1122 : 1086}" height="${story.cardClass === 'collection' ? 1402 : 1448}"></div><figcaption>${esc(story.caption)}</figcaption></figure>` : '';
const related = story => story.relatedSlugs?.length
  ? story.relatedSlugs.map(slug => stories.find(item => item.slug === slug))
  : [...stories.filter(item => item.slug !== story.slug && item.topic === story.topic), ...stories.filter(item => item.topic !== story.topic)].slice(0, 2);
const articlePage = story => `${head(story.title, story.excerpt)}${header(story.topic)}<main id="main" class="article-main"><a class="back-link" href="/journal/">${backArrow} All stories</a><article><header class="article-heading"><div class="story-meta"><a href="${topicLink(topicOf(story))}">${esc(topicOf(story).short)}</a><span>${readTime(story)} min read</span><span>${esc(story.format || 'Essay')}</span></div><h1>${esc(story.title)}</h1><p class="article-deck">${esc(story.excerpt)}</p></header>${articleCover(story)}<div class="article-body"><p class="intro">${esc(story.intro)}</p>${story.sections.map(section => `<section><h2>${esc(section.heading)}</h2>${section.paragraphs.map(paragraph => `<p>${esc(paragraph)}</p>`).join('')}${section.list?.length ? `<ul>${section.list.map(item => `<li>${esc(item)}</li>`).join('')}</ul>` : ''}${sectionSources(story, section)}</section>`).join('')}${story.takeaway ? `<aside class="reading-takeaway" aria-label="Take this with you"><p class="eyebrow">Take this with you</p><p>${esc(story.takeaway)}</p></aside>` : ''}${story.closingQuestion ? `<section class="reader-question"><h2>One question to keep</h2><p>${esc(story.closingQuestion)}</p></section>` : ''}${story.sources?.length ? `<details class="article-sources"><summary>Sources & further reading</summary><ul>${story.sources.map(source => `<li><a href="${esc(source.url)}" target="_blank" rel="noopener noreferrer">${esc(source.label)} ${externalArrow}</a>${source.supports ? `<p>${esc(source.supports)}</p>` : ''}</li>`).join('')}</ul></details>` : ''}<p class="draft-note">Ocharo Blog · Preview edition · Editorial draft</p></div></article><section class="related-stories" aria-labelledby="related-title"><div class="section-heading"><h2 id="related-title">Keep reading</h2><a class="text-link archive-link" href="/journal/">All stories ${arrow}</a></div><div class="stories-grid">${related(story).map(card).join('')}</div></section></main>${footer()}`;

await mkdir(join(outputDir, 'journal'), { recursive: true });
await writeFile(join(outputDir, 'index.html'), home());
await writeFile(join(outputDir, 'journal/index.html'), archivePage());
for (const topic of topics) {
  const folder = join(outputDir, 'topics', topic.key);
  await mkdir(folder, { recursive: true });
  await writeFile(`${folder}/index.html`, topicPage(topic));
}
for (const story of stories) {
  const folder = join(outputDir, 'journal', story.slug);
  await mkdir(folder, { recursive: true });
  await writeFile(`${folder}/index.html`, articlePage(story));
}
await writeFile(join(outputDir, 'robots.txt'), ['User-agent: *', 'Disallow: /', ''].join('\n'));
console.log(`Ocharo Blog generated: homepage, archive, ${topics.length} sections, ${stories.length} articles.`);
