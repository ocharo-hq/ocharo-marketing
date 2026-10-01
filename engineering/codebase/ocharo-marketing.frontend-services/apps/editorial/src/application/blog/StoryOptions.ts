import { blogApi as api } from '../../integration/blog';

/** Titles and addresses of every story, for the related-story pickers. */
export async function loadStoryOptions(): Promise<
  { id: string; slug: string; title: string; language: string; translationKey: string | null; status: string }[]
> {
  const result = await api.list({});
  return result.ok
    ? result.value.map((a) => ({
        id: a.id,
        slug: a.slug,
        title: a.title,
        language: a.language,
        translationKey: a.translationKey,
        status: a.status,
      }))
    : [];
}
