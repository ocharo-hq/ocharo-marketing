/** English source messages for Ocharo operator chrome. Authored content is never passed here. */
export type UiLocale = "uz" | "ru" | "en";
export const uiMessages: Readonly<
  Record<string, Readonly<{ uz: string; ru: string }>>
> = {
  "Check your Ocharo connection": { uz: "Ocharo ulanishini tekshiring", ru: "Проверьте подключение к Ocharo" },
  "Open app in a new tab": { uz: "Ilovani yangi oynada ochish", ru: "Открыть приложение в новой вкладке" },
  "Open Ocharo Desk": { uz: "Ocharo Desk’ni ochish", ru: "Открыть Ocharo Desk" },
  "Reconnection opens a new tab. Your unsaved edits stay here.": { uz: "Qayta ulanish yangi oynada ochiladi. Saqlanmagan o‘zgarishlaringiz shu yerda qoladi.", ru: "Подключение откроется в новой вкладке. Несохранённые изменения останутся здесь." },
  "Signed in to Ocharo": { uz: "Ocharo hisobiga kirilgan", ru: "Вход в Ocharo выполнен" },
  "Reconnect to Ocharo": { uz: "Ocharo hisobiga qayta ulaning", ru: "Подключитесь к Ocharo снова" },
  "Use your Ocharo account across all apps.": { uz: "Barcha ilovalarda Ocharo hisobingizdan foydalaning.", ru: "Используйте аккаунт Ocharo во всех приложениях." },
  "Reconnect": { uz: "Qayta ulanish", ru: "Подключиться снова" },
  "Your Ocharo account changed. Reload this app before continuing.": { uz: "Ocharo hisobingiz o‘zgardi. Davom etishdan oldin ilovani qayta yuklang.", ru: "Аккаунт Ocharo изменился. Перезагрузите приложение перед продолжением." },
  'Preview unavailable': {uz:'Ko‘rinish mavjud emas',ru:'Предпросмотр недоступен'},
  'Retry preview': {uz:'Ko‘rinishni qayta yuklash',ru:'Повторить предпросмотр'},
  'Preview loading': {uz:'Ko‘rinish yuklanmoqda',ru:'Загрузка предпросмотра'},
  'Enquiries unavailable': {uz:'Murojaatlar mavjud emas',ru:'Обращения недоступны'},
  'Activity unavailable': {uz:'Faoliyat mavjud emas',ru:'История недоступна'},
  'Enquiry activity': {uz:'Murojaat faoliyati',ru:'История обращения'},

  "Sign in again. Your edits are preserved.": { uz: "Qayta kiring. O‘zgarishlaringiz saqlanib turibdi.", ru: "Войдите снова. Ваши изменения сохранены." },
  "Your access changed. Your edits are preserved.": { uz: "Ruxsatingiz o‘zgardi. O‘zgarishlaringiz saqlanib turibdi.", ru: "Ваш доступ изменился. Ваши изменения сохранены." },
  "Review the entered values. Your edits are preserved.": { uz: "Kiritilgan qiymatlarni tekshiring. O‘zgarishlaringiz saqlanib turibdi.", ru: "Проверьте введённые значения. Ваши изменения сохранены." },
  "This item is unavailable. Your edits are preserved.": { uz: "Bu element mavjud emas. O‘zgarishlaringiz saqlanib turibdi.", ru: "Этот элемент недоступен. Ваши изменения сохранены." },
  "The request could not complete. Your edits are preserved; try again.": { uz: "So‘rov bajarilmadi. O‘zgarishlaringiz saqlanib turibdi; qayta urinib ko‘ring.", ru: "Запрос не выполнен. Ваши изменения сохранены; попробуйте снова." },
  'Needs attention': { uz: 'E’tibor kerak', ru: 'Требует внимания' },
  'Saved successfully. The current view could not refresh. Your local edits are preserved.': { uz: 'Saqlandi. Ko‘rinishni yangilab bo‘lmadi. Mahalliy o‘zgarishlaringiz saqlanib qoldi.', ru: 'Сохранено. Не удалось обновить представление. Ваши локальные изменения сохранены.' },
  "Could not complete the request": { uz: "So‘rovni bajarib bo‘lmadi", ru: "Не удалось выполнить запрос" },
  "Content unavailable": { uz: "Ma’lumot mavjud emas", ru: "Данные недоступны" },
  "This operation no longer matches the current state. Your data is preserved; check the status.":
    {
      uz: "Amal mavjud holat bilan mos kelmadi. Ma’lumotlar saqlanib turibdi; holatni tekshiring.",
      ru: "Операция больше не соответствует текущему состоянию. Данные сохранены; проверьте статус.",
    },
  "Manual publishing; no verified automatic connection": {
    uz: "Qo‘lda joylash; tasdiqlangan avtomatik ulanish mavjud emas",
    ru: "Ручная публикация; проверенного автоматического подключения нет",
  },
  "Ocharo editorial app; separate from social delivery": {
    uz: "Ocharo tahririy ilovasi; ijtimoiy tarmoq yuborishidan alohida",
    ru: "Редакционное приложение Ocharo; отдельно от отправки в соцсети",
  },
  " revision": {
    uz: "-talqin",
    ru: " версия",
  },
  " revision ·": {
    uz: "-talqin ·",
    ru: " версия ·",
  },
  " revision. Save and review the post separately after adding it.": {
    uz: "-talqin. Qo‘shilgach postni alohida saqlang va tekshiring.",
    ru: " версия. После добавления отдельно сохраните и проверьте пост.",
  },
  " connection": {
    uz: "-ulanish",
    ru: " подключение",
  },
  " variant": {
    uz: "-variant",
    ru: " вариант",
  },
  ". Editions link to each other with hreflang; Uzbek uses /uz/ and Russian /ru/.":
    {
      uz: ". Tahrirlar bir-biriga hreflang bilan bog‘lanadi; o‘zbekcha /uz/, ruscha /ru/ ostida.",
      ru: ". Языковые версии связаны через hreflang; узбекская — /uz/, русская — /ru/.",
    },
  "· Research": {
    uz: "· Izlanish",
    ru: "· Исследование",
  },
  "· Published:": {
    uz: "· Joylandi:",
    ru: "· Опубликовано:",
  },
  "· Read channel metrics:": {
    uz: "· Kanal ko‘rsatkichlarini o‘qish:",
    ru: "· Чтение метрик канала:",
  },
  "· Destination:": {
    uz: "· Manzil:",
    ru: "· Адрес:",
  },
  "· Owner:": {
    uz: "· Mas’ul:",
    ru: "· Ответственный:",
  },
  "· Last scheduling:": {
    uz: "· oxirgi rejalashtirish:",
    ru: "· Последнее планирование:",
  },
  "· Reach": {
    uz: "· Qamrov",
    ru: "· Охват",
  },
  "· Manual": {
    uz: "· Qo‘lda",
    ru: "· Вручную",
  },
  "· Reference": {
    uz: "· Manba",
    ru: "· Источник",
  },
  "· Scheduling attempt:": {
    uz: "· Rejalashtirish urinishi:",
    ru: "· Попытка планирования:",
  },
  "· Confirmation": {
    uz: "· Tasdiq",
    ru: "· Подтверждение",
  },
  "· Tashkent": {
    uz: "· Toshkent",
    ru: "· Ташкент",
  },
  "· End date is excluded.": {
    uz: "· Yakun sanasi kirmaydi.",
    ru: "· Конечная дата не включается.",
  },
  "Confirmed this week counts when channel confirmation arrived; older posts may be confirmed later. Manual posts use their publication date. An unlinked manual record may describe the same post; the two columns are not added together.":
    {
      uz: "“Haftada tasdiqlandi” kanal tasdig‘i kelgan vaqtni bildiradi; eski post keyin tasdiqlanishi mumkin. Qo‘lda joylanganlar o‘z joylash sanasi bo‘yicha sanaladi. Rejaga bog‘lanmagan qo‘lda yozuv bir xil postni qayd etishi mumkin; ikki ustun qo‘shilmaydi.",
      ru: "«Подтверждено за неделю» учитывает дату подтверждения канала; старые посты могут подтвердиться позже. Для ручных записей используется дата публикации. Несвязанные записи могут относиться к тому же посту, поэтому столбцы не суммируются.",
    },
  "Keep reading (blank — automatic)": {
    uz: "“Keep reading” (bo‘sh — avtomatik)",
    ru: "Читайте далее (пусто — автоматически)",
  },
  "“Take this with you”": {
    uz: "Asosiy xulosa",
    ru: "Главный вывод",
  },
  "(comma separated)": {
    uz: "vergul bilan ajrating",
    ru: "через запятую",
  },
  "): the site uses its logo and links. These values are used only when the Brand hub is unavailable.":
    {
      uz: "): saytda logotip va havolalar o‘sha yerdan olinadi. Bu yerdagi qiymatlar faqat Brend markazi ishlamay qolganda ishlatiladi.",
      ru: "): сайт использует его логотип и ссылки. Эти значения применяются только при недоступности центра бренда.",
    },
  "{name} {fabric} {decoration} {sizes} {price} {wholesale} {brand} — lines with empty values are omitted.":
    {
      uz: "{name} {fabric} {decoration} {sizes} {price} {wholesale} {brand} — bo‘sh qiymatli qator tushib qoladi.",
      ru: "{name} {fabric} {decoration} {sizes} {price} {wholesale} {brand} — строки с пустыми значениями пропускаются.",
    },
  "Single item": {
    uz: "1 dona",
    ru: "За штуку",
  },
  "Choose 1–3 source images. Visible details and caption ideas are suggested. Images do not verify fabric composition, sizes or prices.":
    {
      uz: "1–3 manba tasvirni tanlang. Ko‘rinadigan belgilar va matn g‘oyasi taklif qilinadi. Mato tarkibi, o‘lcham va narx rasm bilan tasdiqlanmaydi.",
      ru: "Выберите 1–3 исходных изображения. Будут предложены видимые детали и идеи текста. Состав ткани, размеры и цены по изображению не подтверждаются.",
    },
  "1. Product": {
    uz: "1. Mahsulot",
    ru: "1. Товар",
  },
  "2. Template": {
    uz: "2. Shablon",
    ru: "2. Шаблон",
  },
  "3. Text and review": {
    uz: "3. Matn va tekshirish",
    ru: "3. Текст и проверка",
  },
  "30 days": {
    uz: "30 kun",
    ru: "30 дней",
  },
  "3D garment studio": {
    uz: "3D kiyim studiyasi",
    ru: "Студия 3D-одежды",
  },
  "4:5 format": {
    uz: "4:5 format",
    ru: "Формат 4:5",
  },
  "7 days": {
    uz: "7 kun",
    ru: "7 дней",
  },
  "90 days": {
    uz: "90 kun",
    ru: "90 дней",
  },
  "A or B": {
    uz: "A yoki B",
    ru: "А или Б",
  },
  "About app versions": {
    uz: "Ilova versiyalari haqida",
    ru: "О версиях приложения",
  },
  "Activity unavailable.": {
    uz: "Faollik tarixi mavjud emas.",
    ru: "История действий недоступна.",
  },
  "Add image provenance": {
    uz: "Tasvir kelib chiqishini qo‘shish",
    ru: "Добавить происхождение изображения",
  },
  "Add object record": {
    uz: "Obyekt yozuvini qo‘shish",
    ru: "Добавить запись об объекте",
  },
  "AI suggestion": {
    uz: "AI taklifi",
    ru: "Предложение ИИ",
  },
  "AI image": {
    uz: "AI tasvir",
    ru: "Изображение ИИ",
  },
  Account: {
    uz: "Akkaunt",
    ru: "Аккаунт",
  },
  "Account and destination": {
    uz: "Akkaunt va manzil",
    ru: "Аккаунт и адрес",
  },
  "Account:": {
    uz: "Akkaunt:",
    ru: "Аккаунт:",
  },
  "Verify account": {
    uz: "Akkauntni tekshirish",
    ru: "Проверить аккаунт",
  },
  "All follow-ups": {
    uz: "Barcha keyingi aloqalar",
    ru: "Все последующие контакты",
  },
  "All publications": {
    uz: "Barcha nashrlar",
    ru: "Все публикации",
  },
  "All statuses": {
    uz: "Barcha holatlar",
    ru: "Все статусы",
  },
  "The operation failed.": {
    uz: "Amal bajarilmadi.",
    ru: "Операция не выполнена.",
  },
  "App:": {
    uz: "Ilova:",
    ru: "Приложение:",
  },
  "Apply creates a new image. Captions and credits stay with this draft. Save the story when ready.":
    {
      uz: "Qo‘llash yangi tasvir yaratadi. Izoh va muallif ma’lumotlari qoralamada qoladi. Tayyor bo‘lgach hikoyani saqlang.",
      ru: "Применение создаёт новое изображение. Подписи и авторство сохраняются в черновике. Сохраните статью, когда закончите.",
    },
  "Apply filters": {
    uz: "Filtrlarni qo‘llash",
    ru: "Применить фильтры",
  },
  "Apply or cancel image edits before saving.": {
    uz: "Saqlashdan oldin tasvir tahririni qo‘llang yoki bekor qiling.",
    ru: "Перед сохранением примените или отмените изменения изображения.",
  },
  "Archive encounter": {
    uz: "Arxivdan topilma",
    ru: "Находка в архиве",
  },
  Archive: {
    uz: "Arxiv",
    ru: "Архив",
  },
  Archived: {
    uz: "Arxivda",
    ru: "В архиве",
  },
  "Original AI suggestion": {
    uz: "Asl AI taklifi",
    ru: "Исходное предложение ИИ",
  },
  "Original photo": {
    uz: "Asl foto",
    ru: "Исходное фото",
  },
  "For original photos, matching fabric or craftsmanship details.": {
    uz: "Asl foto, mos mato yoki ish detallari uchun.",
    ru: "Для исходных фото, подходящей ткани или деталей работы.",
  },
  Main: {
    uz: "Asosiy",
    ru: "Основное",
  },
  "Skip to main content": {
    uz: "Asosiy bo‘limga o‘tish",
    ru: "Перейти к основному содержимому",
  },
  "Main sections": {
    uz: "Asosiy bo‘limlar",
    ru: "Основные разделы",
  },
  "Base URL (for RSS and sitemap)": {
    uz: "Asosiy manzil (RSS, sitemap uchun)",
    ru: "Основной адрес (для RSS и sitemap)",
  },
  "Assign a person": {
    uz: "Mas’ulni belgilang",
    ru: "Назначить ответственного",
  },
  "Assign the next conversation, record its outcome, and keep a follow-up date.":
    {
      uz: "Keyingi suhbat uchun mas’ul belgilang, natijasini yozing va keyingi aloqa sanasini saqlang.",
      ru: "Назначьте ответственного за следующий разговор, запишите результат и дату следующего контакта.",
    },
  "Atlas product": {
    uz: "Atlas mahsuloti",
    ru: "Товар для атласа",
  },
  "Atlas PDF": {
    uz: "Atlas PDF",
    ru: "Атлас PDF",
  },
  "Atlas downloaded.": {
    uz: "Atlas yuklab olindi.",
    ru: "Атлас скачан.",
  },
  "Atlas, Wear and Blog share reviewed revisions. Research enters as drafts.": {
    uz: "Atlas, Wear va Blog tekshirilgan talqinlarni ishlatadi. Izlanish qoralama sifatida kiradi.",
    ru: "Atlas, Wear и Blog используют проверенные версии. Исследования импортируются как черновики.",
  },
  "Compare in Atlas; apply the chosen variant in everyday work.": {
    uz: "Atlasda taqqoslang; kundalik ishda tayyor variantni qo‘llang.",
    ru: "Сравнивайте в атласе; применяйте выбранный вариант в работе.",
  },
  "Compare in Atlas →": {
    uz: "Atlasda taqqoslash →",
    ru: "Сравнить в атласе →",
  },
  "View Atlas": {
    uz: "Atlasni ko‘rish",
    ru: "Открыть атлас",
  },
  Audience: {
    uz: "Auditoriya",
    ru: "Аудитория",
  },
  "Set the audience, topics, owner and weekly target. Plans, actual deliveries and manual posts are counted separately.":
    {
      uz: "Auditoriya, mavzular, egasi va haftalik maqsadni belgilang. Reja, haqiqiy yuborish va qo‘lda qayd etilgan postlar alohida hisoblanadi.",
      ru: "Задайте аудиторию, темы, ответственного и недельную цель. Планы, фактические отправки и ручные публикации учитываются отдельно.",
    },
  "Automatic background removal is unavailable on this host. Crop, rotation and resizing are available.":
    {
      uz: "Bu serverda fonni avtomatik olib tashlash mavjud emas. Qirqish, aylantirish va o‘lchamni o‘zgartirish mumkin.",
      ru: "Автоматическое удаление фона на этом сервере недоступно. Доступны обрезка, поворот и изменение размера.",
    },
  "Automatic publishing is currently off. Manual publishing plans are saved.": {
    uz: "Avtomatik joylash hozir o‘chirilgan. Qo‘lda joylash rejasi saqlanadi.",
    ru: "Автоматическая публикация отключена. Планы ручной публикации сохраняются.",
  },
  "Automatic mode": {
    uz: "Avtomatik rejim",
    ru: "Автоматический режим",
  },
  "Automatic delivery": {
    uz: "Avtomatik yuborish",
    ru: "Автоматическая отправка",
  },
  Automation: {
    uz: "Avtomatika",
    ru: "Автоматизация",
  },
  "Automation settings saved.": {
    uz: "Avtomatika sozlamalari saqlandi.",
    ru: "Настройки автоматизации сохранены.",
  },
  "Prepare the card image first": {
    uz: "Avval karta tasvirini tayyorlang",
    ru: "Сначала подготовьте изображение карточки",
  },
  "Add the source image to the library first.": {
    uz: "Avval manba tasvirini kutubxonaga qo‘shing.",
    ru: "Сначала добавьте исходное изображение в библиотеку.",
  },
  "Save as a draft first.": {
    uz: "Avval qoralama sifatida saqlang.",
    ru: "Сначала сохраните черновик.",
  },
  "The previous state is preserved below.": {
    uz: "Avvalgi holat quyida saqlangan.",
    ru: "Предыдущее состояние сохранено ниже.",
  },
  "The exact text and images to send": {
    uz: "Aynan yuboriladigan matn va tasvirlar",
    ru: "Точный текст и изображения для отправки",
  },
  Completed: {
    uz: "Bajarilgan",
    ru: "Выполнено",
  },
  Running: {
    uz: "Bajarilmoqda",
    ru: "Выполняется",
  },
  "All topics": {
    uz: "Barcha bo‘limlar",
    ru: "Все разделы",
  },
  "All plans": {
    uz: "Barcha rejalar",
    ru: "Все планы",
  },
  "All saved": {
    uz: "Barcha saqlangan",
    ru: "Все сохранённые",
  },
  "All images": {
    uz: "Barcha tasvirlar",
    ru: "Все изображения",
  },
  "All languages": {
    uz: "Barcha tillar",
    ru: "Все языки",
  },
  All: {
    uz: "Barchasi",
    ru: "Все",
  },
  Cancelled: {
    uz: "Bekor qilingan",
    ru: "Отменено",
  },
  Cancel: {
    uz: "Bekor qilish",
    ru: "Отмена",
  },
  "One product, different expressions.": {
    uz: "Bir mahsulot, turli ifoda.",
    ru: "Один товар, разные образы.",
  },
  "One product. One new post.": {
    uz: "Bir mahsulot. Bir yangi post.",
    ru: "Один товар. Один новый пост.",
  },
  "First post": {
    uz: "Birinchi post",
    ru: "Первый пост",
  },
  "Create the first post": {
    uz: "Birinchi postni yaratish",
    ru: "Создать первый пост",
  },
  Blog: {
    uz: "Blog",
    ru: "Блог",
  },
  "Blog stories": {
    uz: "Blog hikoyalari",
    ru: "Статьи блога",
  },
  "A post draft for the blog story was created.": {
    uz: "Blog hikoyasi uchun post qoralamasi yaratildi.",
    ru: "Создан черновик поста для статьи блога.",
  },
  "Blog studio": {
    uz: "Blog studiyasi",
    ru: "Студия блога",
  },
  "Unable to connect to the blog service. Check that it is running.": {
    uz: "Blog xizmatiga ulanib bo‘lmadi. Xizmat ishlayotganini tekshiring.",
    ru: "Не удалось подключиться к сервису блога. Проверьте, запущен ли он.",
  },
  Topic: {
    uz: "Bo‘lim",
    ru: "Раздел",
  },
  "Topic deleted.": {
    uz: "Bo‘lim o‘chirildi.",
    ru: "Раздел удалён.",
  },
  "Delete this topic? Topics containing stories cannot be deleted.": {
    uz: "Bo‘lim o‘chirilsinmi? Ichida hikoya bo‘lsa, rad etiladi.",
    ru: "Удалить раздел? Разделы со статьями удалить нельзя.",
  },
  "Topic heading": {
    uz: "Bo‘lim sarlavhasi",
    ru: "Заголовок раздела",
  },
  Topics: {
    uz: "Bo‘limlar",
    ru: "Разделы",
  },
  "Delete topic": {
    uz: "Bo‘limni o‘chirish",
    ru: "Удалить раздел",
  },
  "Fill available slots": {
    uz: "Bo‘sh vaqtlarga joylash",
    ru: "Заполнить свободные слоты",
  },
  "Borrow the idea.": {
    uz: "G‘oyadan foydalaning.",
    ru: "Возьмите идею на заметку.",
  },
  "Hero image": {
    uz: "Bosh rasm",
    ru: "Главное изображение",
  },
  "Homepage title": {
    uz: "Bosh sahifa sarlavhasi (title)",
    ru: "Заголовок главной страницы",
  },
  "Homepage description": {
    uz: "Bosh sahifa tavsifi",
    ru: "Описание главной страницы",
  },
  "Featured homepage story": {
    uz: "Bosh sahifadagi asosiy hikoya",
    ru: "Главная статья",
  },
  "Try another name or change the filter.": {
    uz: "Boshqa nomni sinab ko‘ring yoki filtrni o‘zgartiring.",
    ru: "Попробуйте другое название или измените фильтр.",
  },
  Brand: {
    uz: "Brend",
    ru: "Бренд",
  },
  "Brand hub connected (revision": {
    uz: "Brend markazi ulangan (nashr",
    ru: "Центр бренда подключён (версия",
  },
  "Bring a concept into review": {
    uz: "G‘oyani ko‘rib chiqishga yuboring",
    ru: "Передать концепцию на рассмотрение",
  },
  "Unable to open this section.": {
    uz: "Bu bo‘limni ochib bo‘lmadi.",
    ru: "Не удалось открыть раздел.",
  },
  "No manual observations for this period. Missing data is not counted as zero.":
    {
      uz: "Bu davr uchun qo‘lda kuzatuv yo‘q. Natija nol deb hisoblanmaydi.",
      ru: "За этот период нет ручных наблюдений. Отсутствие данных не считается нулём.",
    },
  "This story:": {
    uz: "Bu hikoya:",
    ru: "Эта статья:",
  },
  "This channel does not support the selected post format.": {
    uz: "Bu kanal tanlangan post formatini qo‘llamaydi.",
    ru: "Канал не поддерживает выбранный формат поста.",
  },
  "Automatic delivery is not yet available for this channel.": {
    uz: "Bu kanalda avtomatik yuborish yo‘li hali mavjud emas.",
    ru: "Автоматическая отправка в этот канал пока недоступна.",
  },
  "Delivery to this channel is not enabled.": {
    uz: "Bu kanalga yuborish yoqilmagan.",
    ru: "Отправка в этот канал не включена.",
  },
  "This target does not create a schedule. Set times in Automation.": {
    uz: "Bu maqsad avtomatik jadval yaratmaydi. Vaqtlarni Avtomatika bo‘limida belgilang.",
    ru: "Эта цель не создаёт расписание. Задайте время в разделе автоматизации.",
  },
  "This plan already has delivery history. Create a new plan for a new attempt.":
    {
      uz: "Bu rejada yuborish tarixi mavjud. Yangi niyat uchun yangi reja yarating.",
      ru: "У этого плана уже есть история отправок. Для новой попытки создайте новый план.",
    },
  "This page was not found.": {
    uz: "Bu sahifa topilmadi.",
    ru: "Страница не найдена.",
  },
  "This request has started. A paid request with an unknown outcome is not retried automatically.":
    {
      uz: "Bu so‘rov boshlangan. Holati noma’lum bo‘lsa pulli so‘rov avtomatik takrorlanmaydi.",
      ru: "Запрос уже начат. Платный запрос с неизвестным результатом автоматически не повторяется.",
    },
  "This confirmation authorizes this exact post to this channel at the scheduled time.":
    {
      uz: "Bu tasdiq belgilangan vaqtda aynan shu postni shu kanalga yuborishga ruxsat beradi.",
      ru: "Это подтверждение разрешает отправку именно этого поста в этот канал в указанное время.",
    },
  "This button reads channel data. It does not send a post.": {
    uz: "Bu tugma kanal ma’lumotini o‘qiydi. Post yubormaydi.",
    ru: "Кнопка читает данные канала и не отправляет пост.",
  },
  "This X read request may use credits. No new post will be sent.": {
    uz: "Bu X o‘qish so‘rovi kredit sarflashi mumkin. Yangi post yuborilmaydi.",
    ru: "Чтение данных X может расходовать кредиты. Новый пост не будет отправлен.",
  },
  "This delivery method supports only one image.": {
    uz: "Bu yuborish yo‘li faqat bitta tasvirni qo‘llaydi.",
    ru: "Этот способ отправки поддерживает одно изображение.",
  },
  Today: {
    uz: "Bugun",
    ru: "Сегодня",
  },
  "These plans are not authorized for delivery. Record manually published posts in Results.":
    {
      uz: "Bular yuborishga ruxsat berilmagan rejalar. Qo‘lda joylangan bo‘lsa, Natijalar bo‘limida qayd eting.",
      ru: "У этих планов нет разрешения на отправку. Ручные публикации отметьте в результатах.",
    },
  Orders: {
    uz: "Buyurtmalar",
    ru: "Заказы",
  },
  "Cancel image edits": {
    uz: "Tasvir tahririni bekor qilish",
    ru: "Отменить изменения изображения",
  },
  Catalogue: {
    uz: "Katalog",
    ru: "Каталог",
  },
  "Catalogue ↗": {
    uz: "Katalog ↗",
    ru: "Каталог ↗",
  },
  Wed: {
    uz: "Chor",
    ru: "Ср",
  },
  characters: {
    uz: "belgi",
    ru: "символов",
  },
  "Check connection": {
    uz: "Ulanishni tekshirish",
    ru: "Проверить подключение",
  },
  "Checking package…": {
    uz: "Paket tekshirilmoqda…",
    ru: "Проверка пакета…",
  },
  "Choose a valid local follow-up date and time.": {
    uz: "Keyingi aloqa uchun to‘g‘ri mahalliy sana va vaqtni tanlang.",
    ru: "Выберите корректную местную дату и время следующего контакта.",
  },
  "Choose an existing piece": {
    uz: "Mavjud mahsulotni tanlang",
    ru: "Выберите существующее изделие",
  },
  closed: {
    uz: "yopilgan",
    ru: "закрыто",
  },
  "Connect this desk": {
    uz: "Ushbu ish stolini ulash",
    ru: "Подключить этот кабинет",
  },
  "Connection status is unavailable.": {
    uz: "Ulanish holati mavjud emas.",
    ru: "Состояние подключения недоступно.",
  },
  "Connection status is unavailable. Your unsaved work stays here.": {
    uz: "Ulanish holati mavjud emas. Saqlanmagan ish shu yerda qoladi.",
    ru: "Состояние подключения недоступно. Несохранённая работа остаётся здесь.",
  },
  contacted: {
    uz: "aloqa qilingan",
    ru: "связались",
  },
  "Content type": {
    uz: "Kontent turi",
    ru: "Тип материала",
  },
  "Conversations to continue": {
    uz: "Davom ettiriladigan suhbatlar",
    ru: "Разговоры для продолжения",
  },
  "Could not save.": {
    uz: "Saqlab bo‘lmadi.",
    ru: "Не удалось сохранить.",
  },
  "Could not save. Your changes stay here.": {
    uz: "Saqlab bo‘lmadi. O‘zgarishlaringiz shu yerda qoladi.",
    ru: "Не удалось сохранить. Ваши изменения остаются здесь.",
  },
  "Cover image editing": {
    uz: "Muqova tasvirini tahrirlash",
    ru: "Редактирование обложки",
  },
  "min ·": {
    uz: "daq ·",
    ru: "мин ·",
  },
  "min read": {
    uz: "daq o‘qish",
    ru: "мин чтения",
  },
  Continue: {
    uz: "Davom ettiring",
    ru: "Продолжить",
  },
  Details: {
    uz: "Detallar",
    ru: "Детали",
  },
  "Discard unsaved changes": {
    uz: "Saqlanmagan o‘zgarishlarni bekor qilish",
    ru: "Отменить несохранённые изменения",
  },
  Mon: {
    uz: "Dush",
    ru: "Пн",
  },
  "Due as of": {
    uz: "Muddat holati:",
    ru: "По состоянию на",
  },
  "Due now": {
    uz: "Hozir bajarish kerak",
    ru: "Пора связаться",
  },
  "Deliveries needing attention": {
    uz: "E’tibor talab qiladigan yuborishlar",
    ru: "Отправки, требующие внимания",
  },
  "Earlier activity": {
    uz: "Oldingi faollik",
    ru: "Предыдущие действия",
  },
  "Edit cover image": {
    uz: "Muqova tasvirini tahrirlash",
    ru: "Редактировать обложку",
  },
  "Edit image": {
    uz: "Tasvirni tahrirlash",
    ru: "Редактировать изображение",
  },
  "Editing access": {
    uz: "Tahrirlash huquqi",
    ru: "Доступ к редактированию",
  },
  "Editing and approval access": {
    uz: "Tahrirlash va tasdiqlash huquqi",
    ru: "Доступ к редактированию и утверждению",
  },
  "Editorial publications": {
    uz: "Tahririy nashrlar",
    ru: "Редакционные публикации",
  },
  Export: {
    uz: "Eksport",
    ru: "Экспорт",
  },
  "Enable formatting": {
    uz: "Formatlashni yoqish",
    ru: "Включить форматирование",
  },
  "Showing the latest 250 observations; earlier history is preserved.": {
    uz: "Eng so‘nggi 250 kuzatuv ko‘rsatilgan; oldingi tarix saqlanib turibdi.",
    ru: "Показаны последние 250 наблюдений; более ранняя история сохранена.",
  },
  Latest: {
    uz: "Eng so‘nggisi",
    ru: "Последние",
  },
  Enquiries: {
    uz: "So‘rovlar",
    ru: "Обращения",
  },
  "Enquiry pages": {
    uz: "So‘rov sahifalari",
    ru: "Страницы обращений",
  },
  "Enquiry status": {
    uz: "So‘rov holati",
    ru: "Статус обращения",
  },
  "Text from facts": {
    uz: "Faktlardan matn",
    ru: "Текст из фактов",
  },
  Active: {
    uz: "Faol",
    ru: "Активно",
  },
  Engagement: {
    uz: "Faollik",
    ru: "Вовлечённость",
  },
  "Engagement unknown": {
    uz: "Faollik noma’lum",
    ru: "Вовлечённость неизвестна",
  },
  "Only plans that have not been sent can enter the queue.": {
    uz: "Faqat hali yuborilmagan reja navbatga qo‘shiladi.",
    ru: "В очередь можно добавить только ещё не отправленные планы.",
  },
  "Only text will be sent.": {
    uz: "Faqat matn yuboriladi.",
    ru: "Будет отправлен только текст.",
  },
  "Choose a file": {
    uz: "Fayl tanlash",
    ru: "Выбрать файл",
  },
  "Select a file and enter a source note.": {
    uz: "Fayl va manba izohini kiriting.",
    ru: "Выберите файл и укажите источник.",
  },
  "Follow-up": {
    uz: "Keyingi aloqa",
    ru: "Следующий контакт",
  },
  "Follow-up date ·": {
    uz: "Keyingi aloqa sanasi ·",
    ru: "Дата следующего контакта ·",
  },
  "Follow-up due ·": {
    uz: "Keyingi aloqa muddati ·",
    ru: "Срок следующего контакта ·",
  },
  "Follow-up filter": {
    uz: "Keyingi aloqa filtri",
    ru: "Фильтр следующих контактов",
  },
  "Follow-up owner (name)": {
    uz: "Keyingi aloqa mas’uli (ism)",
    ru: "Ответственный за контакт (имя)",
  },
  "Follow-up saved. No outgoing message or order was created.": {
    uz: "Keyingi aloqa saqlandi. Xabar yuborilmadi va buyurtma yaratilmadi.",
    ru: "Следующий контакт сохранён. Сообщение не отправлено, заказ не создан.",
  },
  "Follow-up:": {
    uz: "Keyingi aloqa:",
    ru: "Следующий контакт:",
  },
  Format: {
    uz: "Format",
    ru: "Формат",
  },
  Formats: {
    uz: "Formatlar",
    ru: "Форматы",
  },
  "Formats · owner": {
    uz: "Formatlar · mas’ul",
    ru: "Форматы · ответственный",
  },
  "Formatted text": {
    uz: "Formatlangan matn",
    ru: "Форматированный текст",
  },
  "Prepare photo": {
    uz: "Foto tayyorlash",
    ru: "Подготовить фото",
  },
  "General enquiry": {
    uz: "Umumiy so‘rov",
    ru: "Общий вопрос",
  },
  "Horizontal offset": {
    uz: "Gorizontal siljish",
    ru: "Сдвиг по горизонтали",
  },
  Guide: {
    uz: "Qo‘llanma",
    ru: "Руководство",
  },
  "Week:": {
    uz: "Hafta:",
    ru: "Неделя:",
  },
  "Confirmed this week": {
    uz: "Haftada tasdiqlandi",
    ru: "Подтверждено за неделю",
  },
  "Weekly target": {
    uz: "Haftalik maqsad",
    ru: "Недельная цель",
  },
  "Weekly slots": {
    uz: "Haftalik vaqtlar",
    ru: "Недельные слоты",
  },
  "Posts per week": {
    uz: "Haftasiga postlar",
    ru: "Постов в неделю",
  },
  "No channel delivery history yet. Planned posts are not included in this list.":
    {
      uz: "Hali kanalga yuborish tarixi yo‘q. Rejadagi postlar bu ro‘yxatga kirmaydi.",
      ru: "Истории отправок в канал пока нет. Запланированные посты сюда не входят.",
    },
  "Not yet observed": {
    uz: "Hali kuzatilmagan",
    ru: "Ещё не наблюдалось",
  },
  "No plans yet": {
    uz: "Hali reja yo‘q",
    ru: "Планов пока нет",
  },
  "No saved analysis yet. Start with the selected image.": {
    uz: "Hali saqlangan tahlil yo‘q. Tanlangan tasvirdan boshlang.",
    ru: "Сохранённого анализа пока нет. Начните с выбранного изображения.",
  },
  "All changes saved": {
    uz: "Hammasi saqlangan",
    ru: "Все изменения сохранены",
  },
  "Requires a confirmed publication, a verified connection and permission to read metrics. Only saved observations appear below.":
    {
      uz: "Haqiqiy joylash tasdig‘i, tekshirilgan ulanish va ko‘rsatkichlarni o‘qish ruxsati kerak. Quyida faqat avval saqlangan kuzatuvlar ko‘rsatiladi.",
      ru: "Нужны подтверждённая публикация, проверенное подключение и разрешение на чтение метрик. Ниже только сохранённые наблюдения.",
    },
  "Close-up of the actual fabric and craftsmanship.": {
    uz: "Haqiqiy mato va ish yaqin fotosi.",
    ru: "Крупный план настоящей ткани и деталей работы.",
  },
  "Connect a channel for real results": {
    uz: "Haqiqiy natijalar uchun kanal kerak",
    ru: "Подключите канал для реальных результатов",
  },
  "Each channel’s purpose": {
    uz: "Har bir kanalning vazifasi",
    ru: "Назначение каждого канала",
  },
  "Every caption, selected image and reviewed variant in one place.": {
    uz: "Har bir matn, tanlangan tasvir va tekshirilgan variant bir joyda.",
    ru: "Все тексты, выбранные изображения и проверенные варианты в одном месте.",
  },
  "Every channel has its own approach.": {
    uz: "Har kanalning o‘z yo‘li bor.",
    ru: "У каждого канала свой подход.",
  },
  "Shows each post’s latest observation without adding repeated cumulative counts. Engagement = (likes + comments + shares + saves) ÷ reach × 100. No percentage is calculated when any value is unknown.":
    {
      uz: "Har postning oxirgi kuzatuvi ko‘rsatiladi; takroriy jamlangan sonlar qo‘shilmaydi. Faollik = (yoqtirish + izoh + ulashish + saqlash) ÷ qamrov × 100. Biror son noma’lum bo‘lsa foiz hisoblanmaydi.",
      ru: "Показано последнее наблюдение каждого поста без суммирования повторных накопленных значений. Вовлечённость = (лайки + комментарии + репосты + сохранения) ÷ охват × 100. При неизвестных значениях процент не рассчитывается.",
    },
  Link: {
    uz: "Havola",
    ru: "Ссылка",
  },
  "Link clicks": {
    uz: "Havola bosishlar",
    ru: "Переходы по ссылке",
  },
  "Link URL": {
    uz: "Havola manzili",
    ru: "Адрес ссылки",
  },
  "Link label": {
    uz: "Havola nomi",
    ru: "Название ссылки",
  },
  "Headings, bold, italic, links, lists and quotes. Use the cover editor for images.":
    {
      uz: "Sarlavhalar, qalin va qiya matn, havolalar, ro‘yxatlar va iqtiboslar. Tasvirlar uchun muqova tahrirchisidan foydalaning.",
      ru: "Заголовки, жирный и курсивный текст, ссылки, списки и цитаты. Для изображений используйте редактор обложки.",
    },
  "Hide activity": {
    uz: "Faollikni yashirish",
    ru: "Скрыть действия",
  },
  Story: {
    uz: "Hikoya",
    ru: "Статья",
  },
  "Story archived.": {
    uz: "Hikoya arxivlandi.",
    ru: "Статья архивирована.",
  },
  "Story preview": {
    uz: "Hikoya ko‘rinishi",
    ru: "Предпросмотр статьи",
  },
  "Story published — visible on the site.": {
    uz: "Hikoya nashr qilindi — saytda ko‘rinadi.",
    ru: "Статья опубликована и видна на сайте.",
  },
  "Story saved.": {
    uz: "Hikoya saqlandi.",
    ru: "Статья сохранена.",
  },
  "Story removed from the site (draft).": {
    uz: "Hikoya saytdan olindi (qoralama).",
    ru: "Статья снята с публикации (черновик).",
  },
  "Story not found.": {
    uz: "Hikoya topilmadi.",
    ru: "Статья не найдена.",
  },
  Stories: {
    uz: "Hikoyalar",
    ru: "Статьи",
  },
  "Story studio": {
    uz: "Hikoyalar studiyasi",
    ru: "Студия статей",
  },
  "Back to stories": {
    uz: "Hikoyalarga qaytish",
    ru: "К статьям",
  },
  Status: {
    uz: "Holat",
    ru: "Статус",
  },
  "Check status": {
    uz: "Holatni tekshirish",
    ru: "Проверить статус",
  },
  "Refresh status": {
    uz: "Holatni yangilash",
    ru: "Обновить статус",
  },
  "Views and engagement data are not available yet. You can continue preparing and manually publishing posts.":
    {
      uz: "Hozir ko‘rishlar yoki faollik haqida ma’lumot yo‘q. Postlarni tayyorlash va qo‘lda joylash davom etishi mumkin.",
      ru: "Данные о просмотрах и вовлечённости пока недоступны. Можно продолжать подготовку и ручную публикацию постов.",
    },
  "Currently a local workspace": {
    uz: "Hozir mahalliy ish stoli",
    ru: "Сейчас это локальный кабинет",
  },
  "Publish now": {
    uz: "Hozir nashr qilish",
    ru: "Опубликовать сейчас",
  },
  "Save for now": {
    uz: "Hozircha saqlash",
    ru: "Пока сохранить",
  },
  "Two actual colour or design variants.": {
    uz: "Ikki haqiqiy rang yoki dizayn varianti.",
    ru: "Два реальных варианта цвета или дизайна.",
  },
  "Image editing capabilities are unavailable. Retry when ready.": {
    uz: "Tasvir tahrirlash imkoniyatlari mavjud emas. Tayyor bo‘lgach qayta urinib ko‘ring.",
    ru: "Инструменты редактирования изображений недоступны. Повторите позже.",
  },
  "Image editing did not finish. Your cover is unchanged. Retry when ready.": {
    uz: "Tasvir tahriri tugamadi. Muqova o‘zgarmadi. Tayyor bo‘lgach qayta urinib ko‘ring.",
    ru: "Редактирование изображения не завершено. Обложка не изменена. Повторите позже.",
  },
  "Import as a draft": {
    uz: "Qoralama sifatida import qilish",
    ru: "Импортировать как черновик",
  },
  "Import failed": {
    uz: "Import bajarilmadi",
    ru: "Импорт не выполнен",
  },
  "Import failed.": {
    uz: "Import bajarilmadi.",
    ru: "Импорт не выполнен.",
  },
  "Import research drafts": {
    uz: "Izlanish qoralamalarini import qilish",
    ru: "Импортировать черновики исследований",
  },
  "Import the PNG and review manifest explicitly exported from Studio. The result is a draft labelled creative visualization. Product facts, rights and publication need their own review.":
    {
      uz: "Studio’dan eksport qilingan PNG va tekshirish manifestini import qiling. Natija ijodiy vizualizatsiya deb belgilangan qoralama bo‘ladi. Mahsulot faktlari, huquqlar va nashr alohida tekshiriladi.",
      ru: "Импортируйте PNG и манифест проверки, экспортированные из Studio. Результат — черновик с пометкой творческой визуализации. Факты о товаре, права и публикация проверяются отдельно.",
    },
  "Import unavailable. Existing drafts are preserved.": {
    uz: "Import mavjud emas. Mavjud qoralamalar saqlangan.",
    ru: "Импорт недоступен. Существующие черновики сохранены.",
  },
  "Inbox unavailable.": {
    uz: "So‘rovlar qutisi mavjud emas.",
    ru: "Входящие обращения недоступны.",
  },
  English: {
    uz: "Inglizcha",
    ru: "Английский",
  },
  "Enter Instagram, Telegram or website post results with the date and source. These are operator records, not channel API confirmations.":
    {
      uz: "Instagram, Telegram yoki saytdagi post natijalarini sanasi va manbasi bilan kiriting. Bular operator yozuvlari; kanal API tasdig‘i emas.",
      ru: "Введите результаты постов Instagram, Telegram или сайта с датой и источником. Это записи оператора, а не подтверждения API канала.",
    },
  "Internal note": {
    uz: "Ichki izoh",
    ru: "Внутренняя заметка",
  },
  Quotes: {
    uz: "Iqtiboslar",
    ru: "Цитаты",
  },
  Desk: {
    uz: "Ish stoli",
    ru: "Кабинет",
  },
  "Workspace has not loaded.": {
    uz: "Ish stoli yuklanmagan.",
    ru: "Кабинет не загружен.",
  },
  "Loading workspace…": {
    uz: "Ish stoli yuklanmoqda…",
    ru: "Загрузка кабинета…",
  },
  "Return to the desk and choose a section.": {
    uz: "Ish stoliga qaytib, kerakli bo‘limni tanlang.",
    ru: "Вернитесь в кабинет и выберите раздел.",
  },
  "Back to desk": {
    uz: "Ish stoliga qaytish",
    ru: "В кабинет",
  },
  "Unable to connect to the workspace.": {
    uz: "Ish stoliga ulanib bo‘lmadi.",
    ru: "Не удалось подключиться к кабинету.",
  },
  Started: {
    uz: "Ishga tushdi",
    ru: "Запущено",
  },
  Run: {
    uz: "Ishga tushirish",
    ru: "Запустить",
  },
  "A running queue service does not mean a channel is connected. Each delivery needs a reviewed post, destination and separate authorization.":
    {
      uz: "Ishlayotgan navbat xizmati kanalga ulanganlikni anglatmaydi. Har bir yuborish tasdiqlangan post, manzil va alohida ruxsat talab qiladi.",
      ru: "Работающая очередь не означает подключение канала. Каждая отправка требует проверенного поста, адреса и отдельного разрешения.",
    },
  Optional: {
    uz: "Ixtiyoriy",
    ru: "Необязательно",
  },
  Note: {
    uz: "Izoh",
    ru: "Примечание",
  },
  "Caption (figcaption)": {
    uz: "Izoh (figcaption)",
    ru: "Подпись (figcaption)",
  },
  "The note must not exceed 2000 characters.": {
    uz: "Izoh 2000 belgidan oshmasin.",
    ru: "Примечание не должно превышать 2000 символов.",
  },
  "Assess interest in colours or designs through comments and replies.": {
    uz: "Izoh va javoblar orqali rang yoki dizayn qiziqishini baholang.",
    ru: "Оценивайте интерес к цветам и дизайну по комментариям и ответам.",
  },
  Comments: {
    uz: "Izohlar",
    ru: "Комментарии",
  },
  "Save schedule": {
    uz: "Jadvalni saqlash",
    ru: "Сохранить расписание",
  },
  Cumulative: {
    uz: "Jamlangan",
    ru: "Накопленные",
  },
  "Process video": {
    uz: "Jarayon videosi",
    ru: "Видео процесса",
  },
  Replies: {
    uz: "Javoblar",
    ru: "Ответы",
  },
  "Live preview": {
    uz: "Jonli ko‘rinish",
    ru: "Предпросмотр",
  },
  "Live delivery:": {
    uz: "Jonli yuborish:",
    ru: "Отправка в канал:",
  },
  Published: {
    uz: "Nashr qilingan",
    ru: "Опубликовано",
  },
  "Published post": {
    uz: "Joylangan post",
    ru: "Опубликованный пост",
  },
  "Published post URL": {
    uz: "Joylangan post havolasi",
    ru: "Ссылка на опубликованный пост",
  },
  "Published at · Tashkent": {
    uz: "Joylangan vaqt · Toshkent",
    ru: "Опубликовано · Ташкент",
  },
  "Publishing schedule": {
    uz: "Joylash jadvali",
    ru: "Расписание публикаций",
  },
  "Publishing history": {
    uz: "Joylash tarixi",
    ru: "История публикаций",
  },
  "Publication confirmation:": {
    uz: "Joylash tasdig‘i:",
    ru: "Подтверждение публикации:",
  },
  "Reset placement": {
    uz: "Joylashuvni tiklash",
    ru: "Сбросить расположение",
  },
  "JPEG fills transparent areas with white. Choose PNG or WebP to keep transparency.":
    {
      uz: "JPEG shaffof joylarni oq rangga to‘ldiradi. Shaffoflikni saqlash uchun PNG yoki WebP tanlang.",
      ru: "JPEG заполняет прозрачные области белым. Для сохранения прозрачности выберите PNG или WebP.",
    },
  Fri: {
    uz: "Jum",
    ru: "Пт",
  },
  Key: {
    uz: "Kalit",
    ru: "Ключ",
  },
  "at least one section and paragraph": {
    uz: "kamida bitta bo‘lim va paragraf",
    ru: "хотя бы один раздел и абзац",
  },
  Channel: {
    uz: "Kanal",
    ru: "Канал",
  },
  "The channel account and destination have not been verified yet.": {
    uz: "Kanal akkaunti va manzil hali tekshirilmagan.",
    ru: "Аккаунт и адрес канала ещё не проверены.",
  },
  "The channel is read only when this button is pressed.": {
    uz: "Kanal faqat shu tugma bosilganda o‘qiladi.",
    ru: "Данные канала читаются только по нажатию этой кнопки.",
  },
  "Open editing from a channel card. The initial state does not connect a channel or send posts.":
    {
      uz: "Kanal kartasidan tahrirlashni oching. Dastlabki holat kanalga ulanmaydi va hech qanday post yubormaydi.",
      ru: "Откройте редактирование в карточке канала. Начальное состояние не подключает канал и не отправляет посты.",
    },
  "Accepted by channel": {
    uz: "Kanal qabul qilgan",
    ru: "Принято каналом",
  },
  "Channel plan saved. This does not enable delivery.": {
    uz: "Kanal rejasi saqlandi. Bu yuborishni yoqmaydi.",
    ru: "План канала сохранён. Отправка не включается.",
  },
  "Check the channel history. Sending again may duplicate the post; this screen does not retry automatically.":
    {
      uz: "Kanal tarixini tekshiring. Qayta yuborish takroriy post yaratishi mumkin; bu ekran avtomatik qaytarmaydi.",
      ru: "Проверьте историю канала. Повторная отправка может создать дубликат; этот экран не повторяет её автоматически.",
    },
  "Channel confirmation:": {
    uz: "Kanal tasdig‘i:",
    ru: "Подтверждение канала:",
  },
  "Check channel confirmation": {
    uz: "Kanal tasdig‘ini tekshirish",
    ru: "Проверить подтверждение канала",
  },
  "Channel or page URL": {
    uz: "Kanal yoki sahifa havolasi",
    ru: "Ссылка на канал или страницу",
  },
  "Refresh from channel": {
    uz: "Kanaldan yangilash",
    ru: "Обновить из канала",
  },
  Channels: {
    uz: "Kanallar",
    ru: "Каналы",
  },
  "Channel map": {
    uz: "Kanallar xaritasi",
    ru: "Карта каналов",
  },
  "Mark channel active in the plan": {
    uz: "Kanalni rejada faol deb belgilash",
    ru: "Отметить канал активным в плане",
  },
  "The channel account and destination status were checked.": {
    uz: "Kanalning akkaunt va manzil holati tekshirildi.",
    ru: "Состояние аккаунта и адреса канала проверено.",
  },
  "Prepare card": {
    uz: "Karta tayyorlash",
    ru: "Подготовить карточку",
  },
  Card: {
    uz: "Kartochka",
    ru: "Карточка",
  },
  "Card phrase": {
    uz: "Kartochka iborasi",
    ru: "Фраза для карточки",
  },
  "Catalogue disconnected": {
    uz: "Katalog ulanmagan",
    ru: "Каталог не подключён",
  },
  "Catalogue attempt:": {
    uz: "Katalog urinishi:",
    ru: "Попытка обновления каталога:",
  },
  "Catalogue and scheduling": {
    uz: "Katalog va rejalashtirish",
    ru: "Каталог и планирование",
  },
  "Automatically create a post draft when a new product appears in the catalogue":
    {
      uz: "Katalogda yangi mahsulot paydo bo‘lsa, avtomatik post qoralamasi yaratilsin",
      ru: "Автоматически создавать черновик поста при появлении нового товара в каталоге",
    },
  "Catalogue products (below the story)": {
    uz: "Katalogdagi mahsulotlar (hikoya ostida)",
    ru: "Товары каталога (под статьёй)",
  },
  "Import catalogue products, create post drafts in one click and place approved posts into weekly slots. Scheduling creates a plan only; delivery needs a verified account and separate queue authorization.":
    {
      uz: "Katalogdan mahsulotlarni oling, bir bosishda post qoralamalarini yarating va tasdiqlanganlarini haftalik jadvalga joylang. Jadval faqat reja tuzadi: tarmoqqa yuborish uchun tasdiqlangan hisob va alohida navbat tasdig‘i kerak.",
      ru: "Импортируйте товары, создавайте черновики одним нажатием и размещайте проверенные посты в недельных слотах. Расписание создаёт только план; отправка требует проверенного аккаунта и отдельного разрешения.",
    },
  "Refresh from catalogue": {
    uz: "Katalogdan yangilash",
    ru: "Обновить из каталога",
  },
  "Catalogue refresh interval in hours (0 — off)": {
    uz: "Katalogni har necha soatda yangilash (0 — o‘chiq)",
    ru: "Обновление каталога в часах (0 — выключено)",
  },
  "No overdue plans.": {
    uz: "Kechikkan reja yo‘q.",
    ru: "Просроченных планов нет.",
  },
  "Wide feed": {
    uz: "Keng lenta",
    ru: "Широкая лента",
  },
  "Next 7 days:": {
    uz: "Keyingi 7 kun:",
    ru: "Следующие 7 дней:",
  },
  "Next week": {
    uz: "Keyingi hafta",
    ru: "Следующая неделя",
  },
  "For the next analysis": {
    uz: "Keyingi tahlil uchun",
    ru: "Для следующего анализа",
  },
  Introduction: {
    uz: "Kirish",
    ru: "Вступление",
  },
  "Worn look": {
    uz: "Kiyilgan ko‘rinish",
    ru: "Образ на модели",
  },
  "Ready for review": {
    uz: "Ko‘rib chiqishga tayyor",
    ru: "Готово к проверке",
  },
  "Regenerate the image if the appearance changed.": {
    uz: "Ko‘rinish o‘zgargan bo‘lsa, tasvirni qayta tayyorlang.",
    ru: "Если оформление изменилось, создайте изображение заново.",
  },
  "Preparing preview…": {
    uz: "Ko‘rinish tayyorlanmoqda…",
    ru: "Подготовка предпросмотра…",
  },
  "Visual atlas": {
    uz: "Ko‘rinishlar atlasi",
    ru: "Атлас оформления",
  },
  View: {
    uz: "Ko‘rish",
    ru: "Открыть",
  },
  "Track viewing duration and shares.": {
    uz: "Ko‘rish davomiyligi va ulashishlarni kuzating.",
    ru: "Следите за длительностью просмотров и репостами.",
  },
  Views: {
    uz: "Ko‘rishlar",
    ru: "Просмотры",
  },
  "Metrics appear when actual observations exist. Missing data is not zero.": {
    uz: "Ko‘rsatkichlar haqiqiy kuzatuv mavjud bo‘lganda ko‘rinadi. Yetishmayotgan ma’lumot nolga teng emas.",
    ru: "Метрики появляются при наличии реальных наблюдений. Отсутствие данных не равно нулю.",
  },
  "Read metrics": {
    uz: "Ko‘rsatkichlarni o‘qish",
    ru: "Получить метрики",
  },
  "Content library": {
    uz: "Kontent kutubxonasi",
    ru: "Библиотека материалов",
  },
  Day: {
    uz: "Kun",
    ru: "День",
  },
  Days: {
    uz: "Kunlar",
    ru: "Дни",
  },
  "Choose from library": {
    uz: "Kutubxonadan tanlash",
    ru: "Выбрать из библиотеки",
  },
  "Choose from library…": {
    uz: "Kutubxonadan tanlash…",
    ru: "Выбрать из библиотеки…",
  },
  "Choose a library image or prepare a new card.": {
    uz: "Kutubxonadan tasvir tanlang yoki yangi kartani tayyorlang.",
    ru: "Выберите изображение из библиотеки или подготовьте новую карточку.",
  },
  "Choose library image ·": {
    uz: "Kutubxonadan tasvir tanlash ·",
    ru: "Выбрать изображение из библиотеки ·",
  },
  "Add to library": {
    uz: "Kutubxonaga qo‘shish",
    ru: "Добавить в библиотеку",
  },
  "Observed:": {
    uz: "Kuzatildi:",
    ru: "Наблюдение:",
  },
  Observed: {
    uz: "Kuzatilgan",
    ru: "Зафиксировано",
  },
  "Observed at · Tashkent": {
    uz: "Kuzatilgan vaqt · Toshkent",
    ru: "Время наблюдения · Ташкент",
  },
  "Not observed": {
    uz: "Kuzatilmagan",
    ru: "Не зафиксировано",
  },
  "Observation period": {
    uz: "Kuzatuv davri",
    ru: "Период наблюдений",
  },
  "Add observation": {
    uz: "Kuzatuv qo‘shish",
    ru: "Добавить наблюдение",
  },
  "No observation date": {
    uz: "Kuzatuv sanasi yo‘q",
    ru: "Нет даты наблюдения",
  },
  "Observation history": {
    uz: "Kuzatuv tarixi",
    ru: "История наблюдений",
  },
  "No observation": {
    uz: "Kuzatuv yo‘q",
    ru: "Нет наблюдения",
  },
  "Save observation": {
    uz: "Kuzatuvni saqlash",
    ru: "Сохранить наблюдение",
  },
  "Loading activity…": {
    uz: "Faollik yuklanmoqda…",
    ru: "Загрузка действий…",
  },
  "Loading enquiries…": {
    uz: "So‘rovlar yuklanmoqda…",
    ru: "Загрузка обращений…",
  },
  "Loading image tools…": {
    uz: "Tasvir vositalari yuklanmoqda…",
    ru: "Загрузка инструментов изображения…",
  },
  "Loading original image dimensions…": {
    uz: "Asl tasvir o‘lchamlari yuklanmoqda…",
    ru: "Загрузка исходных размеров изображения…",
  },
  "Local workspace": {
    uz: "Mahalliy ish stoli",
    ru: "Локальный кабинет",
  },
  Logo: {
    uz: "Logotip",
    ru: "Логотип",
  },
  "Logo, colours, links": {
    uz: "Logotip, ranglar, havolalar",
    ru: "Логотип, цвета, ссылки",
  },
  Unavailable: {
    uz: "Mavjud emas",
    ru: "Недоступно",
  },
  "No data": {
    uz: "Ma’lumot yo‘q",
    ru: "Нет данных",
  },
  "Leave unknown values blank. Enter 0 only for a real zero. Enquiries and orders are linked only as verified by you.":
    {
      uz: "Ma’lumot yo‘q bo‘lsa bo‘sh qoldiring. Haqiqiy nol bo‘lsa 0 kiriting. So‘rov va buyurtmalar faqat siz tekshirgan bog‘lanishdir.",
      ru: "Неизвестные значения оставьте пустыми. Введите 0 только для настоящего нуля. Связи с обращениями и заказами подтверждаете вы.",
    },
  "Your data is preserved. Try again.": {
    uz: "Ma’lumotlar o‘chirilmagan. Qayta urinib ko‘ring.",
    ru: "Данные сохранены. Попробуйте ещё раз.",
  },
  "Local status refreshes every 30 seconds. Unapproved plans are not sent; deliveries with unknown outcomes are not retried automatically.":
    {
      uz: "Mahalliy holat har 30 soniyada yangilanadi. Tasdiqlanmagan reja yuborilmaydi; noma’lum yuborish avtomatik takrorlanmaydi.",
      ru: "Локальный статус обновляется каждые 30 секунд. Неподтверждённые планы не отправляются, неизвестные результаты автоматически не повторяются.",
    },
  "Local workspace · Tashkent": {
    uz: "Mahalliy ish stoli · Toshkent",
    ru: "Локальный кабинет · Ташкент",
  },
  "Start the local service and try again.": {
    uz: "Mahalliy xizmatni ishga tushirib, yana urinib ko‘ring.",
    ru: "Запустите локальный сервис и попробуйте снова.",
  },
  Product: {
    uz: "Mahsulot",
    ru: "Товар",
  },
  "The product takes centre stage.": {
    uz: "Mahsulot — diqqat markazida.",
    ru: "Товар в центре внимания.",
  },
  "Product detail": {
    uz: "Mahsulot detali",
    ru: "Деталь товара",
  },
  "Product facts": {
    uz: "Mahsulot faktlari",
    ru: "Факты о товаре",
  },
  "Choose a product photo, uploaded image or prepared card. Your selection order is preserved.":
    {
      uz: "Mahsulot fotosi, yuklangan tasvir yoki tayyor kartani tanlang. Tartib tanlangan ketma-ketlikda saqlanadi.",
      ru: "Выберите фото товара, загруженное изображение или готовую карточку. Порядок выбора сохраняется.",
    },
  "Write a concise, accurate product caption…": {
    uz: "Mahsulot haqida qisqa va aniq yozing…",
    ru: "Кратко и точно опишите товар…",
  },
  "Paste product source text. Review extracted facts and missing details.": {
    uz: "Mahsulot haqidagi matnni joylang. Topilgan ma’lumot va yetishmayotgan qismlarni ko‘ring.",
    ru: "Вставьте исходное описание товара. Проверьте найденные факты и недостающие данные.",
  },
  "Product card": {
    uz: "Mahsulot kartasi",
    ru: "Карточка товара",
  },
  "Product information": {
    uz: "Mahsulot ma’lumotlari",
    ru: "Информация о товаре",
  },
  "Product front view": {
    uz: "Mahsulot old ko‘rinishi",
    ru: "Вид товара спереди",
  },
  "Product photos, ready templates and your plans in one place.": {
    uz: "Mahsulot rasmlari, tayyor shablonlar va bir joyda saqlangan rejangiz.",
    ru: "Фото товаров, готовые шаблоны и планы в одном месте.",
  },
  "Choose a product": {
    uz: "Mahsulot tanlang",
    ru: "Выберите товар",
  },
  "Choose a product…": {
    uz: "Mahsulot tanlang…",
    ru: "Выберите товар…",
  },
  "Select product": {
    uz: "Mahsulot tanlash",
    ru: "Выбор товара",
  },
  "Enter a product and title.": {
    uz: "Mahsulot va sarlavhani kiriting.",
    ru: "Укажите товар и заголовок.",
  },
  "Product, price, image": {
    uz: "Mahsulot, narx, rasm",
    ru: "Товар, цена, изображение",
  },
  "From product to post": {
    uz: "Mahsulotdan postgacha",
    ru: "От товара к посту",
  },
  "Check that it matches the product. This text does not change the catalogue.":
    {
      uz: "Mahsulotga mosligini tekshiring. Bu matn katalogni o‘zgartirmaydi.",
      ru: "Проверьте соответствие товару. Этот текст не изменяет каталог.",
    },
  Products: {
    uz: "Mahsulotlar",
    ru: "Товары",
  },
  "Products and images": {
    uz: "Mahsulotlar va tasvirlar",
    ru: "Товары и изображения",
  },
  "Select products to create template-based drafts for each one. You approve them.":
    {
      uz: "Mahsulotlarni tanlang — har biri uchun shablon asosida qoralama yaratiladi (tasdiqlash sizda).",
      ru: "Выберите товары: для каждого будет создан черновик по шаблону. Утверждаете их вы.",
    },
  "Find a product": {
    uz: "Mahsulotni topish",
    ru: "Найти товар",
  },
  "Find a product…": {
    uz: "Mahsulotni topish…",
    ru: "Найти товар…",
  },
  "Product front view on a warm light background.": {
    uz: "Mahsulotning old ko‘rinishi iliq och fonda.",
    ru: "Вид товара спереди на тёплом светлом фоне.",
  },
  Source: {
    uz: "Manba",
    ru: "Источник",
  },
  "Source / evidence note": {
    uz: "Manba / dalil izohi",
    ru: "Примечание об источнике / доказательствах",
  },
  "Source provenance": {
    uz: "Manba izi",
    ru: "Происхождение источника",
  },
  "The source has changed or the image is unavailable. This suggestion cannot be accepted now.":
    {
      uz: "Manba izi o‘zgargan yoki tasvir mavjud emas. Bu taklifni hozir qabul qilib bo‘lmaydi.",
      ru: "Источник изменился или изображение недоступно. Сейчас принять предложение нельзя.",
    },
  "Source note": {
    uz: "Manba izohi",
    ru: "Примечание об источнике",
  },
  "Source URL": {
    uz: "Manba manzili",
    ru: "Адрес источника",
  },
  "Review source text": {
    uz: "Manba matnini tekshiring",
    ru: "Проверить исходный текст",
  },
  "Source name": {
    uz: "Manba nomi",
    ru: "Название источника",
  },
  "Source analysis ·": {
    uz: "Manba tahlili ·",
    ru: "Анализ источника ·",
  },
  "Source image": {
    uz: "Manba tasviri",
    ru: "Исходное изображение",
  },
  Sources: {
    uz: "Manbalar",
    ru: "Источники",
  },
  "Sources ·": {
    uz: "Manbalar ·",
    ru: "Источники ·",
  },
  "Sources:": {
    uz: "Manbalar:",
    ru: "Источники:",
  },
  "Remove source": {
    uz: "Manbani o‘chirish",
    ru: "Удалить источник",
  },
  "URL (/journal/…/)": {
    uz: "Manzil (/journal/…/)",
    ru: "Адрес (/journal/…/)",
  },
  Marketing: {
    uz: "Marketing",
    ru: "Маркетинг",
  },
  "Marketing desk": {
    uz: "Marketing ish stoli",
    ru: "Кабинет маркетинга",
  },
  "Marketing workspace is not configured.": {
    uz: "Marketing ish stoli sozlanmagan.",
    ru: "Кабинет маркетинга не настроен.",
  },
  Owner: {
    uz: "Mas’ul",
    ru: "Ответственный",
  },
  "For example: Instagram Insights, reach for this post, read after 24 hours.":
    {
      uz: "Masalan: Instagram Insights, shu postning qamrovi; 24 soatdan so‘ng o‘qildi.",
      ru: "Например: Instagram Insights, охват этого поста через 24 часа.",
    },
  "For example: show the unchanged product against a quiet studio background…":
    {
      uz: "Masalan: mahsulotni o‘zgartirmasdan, sokin studiya fonida ko‘rsating…",
      ru: "Например: покажите товар без изменений на спокойном студийном фоне…",
    },
  "For example: turn colour and cut into a short post caption.": {
    uz: "Masalan: rang va bichimni qisqa post matniga aylantirish.",
    ru: "Например: опишите цвет и крой в коротком тексте поста.",
  },
  Scale: {
    uz: "Masshtab",
    ru: "Масштаб",
  },
  "Scale: 75–105%. Offset: ±12% of card width or ±6% of height; 0.01 = 1%.": {
    uz: "Masshtab: 75–105%. Siljish karta enining ±12% yoki balandligining ±6% chegarasida; 0.01 = 1%.",
    ru: "Масштаб: 75–105%. Сдвиг: ±12% ширины карточки или ±6% высоты; 0,01 = 1%.",
  },
  "Matching PNG": {
    uz: "Mos PNG",
    ru: "Соответствующий PNG",
  },
  "Material: … Size: … Price: …": {
    uz: "Material: … O‘lcham: … Narx: …",
    ru: "Материал: … Размер: … Цена: …",
  },
  "No text entered": {
    uz: "Matn kiritilmagan",
    ru: "Текст не введён",
  },
  "No text entered.": {
    uz: "Matn kiritilmagan.",
    ru: "Текст не введён.",
  },
  "The text changed. Save the reviewed text before using it in a post.": {
    uz: "Matn o‘zgargan. Postda ishlatishdan oldin tekshirilgan matnni saqlang.",
    ru: "Текст изменён. Сохраните проверенный текст перед использованием в посте.",
  },
  "Caption template": {
    uz: "Matn shabloni",
    ru: "Шаблон текста",
  },
  "Text suggestion": {
    uz: "Matn taklifi",
    ru: "Предложение текста",
  },
  "Text reviewed": {
    uz: "Matn tekshirilgan",
    ru: "Текст проверен",
  },
  "Save the text and chosen variant, or leave the page.": {
    uz: "Matn va tanlangan variantni saqlab qoling yoki sahifadan chiqing.",
    ru: "Сохраните текст и выбранный вариант или покиньте страницу.",
  },
  "Text and image services": {
    uz: "Matn va tasvir xizmatlari",
    ru: "Сервисы текста и изображений",
  },
  Texts: {
    uz: "Matnlar",
    ru: "Тексты",
  },
  "Review text": {
    uz: "Matnni tekshirish",
    ru: "Проверить текст",
  },
  "Fabric + product": {
    uz: "Mato + mahsulot",
    ru: "Ткань + товар",
  },
  "Fabric photo": {
    uz: "Mato fotosi",
    ru: "Фото ткани",
  },
  "Fabric photo required": {
    uz: "Mato fotosi kerak",
    ru: "Нужно фото ткани",
  },
  "Choose fabric": {
    uz: "Mato tanlang",
    ru: "Выберите ткань",
  },
  "Choose an existing product image and save your first post.": {
    uz: "Mavjud mahsulot rasmini tanlang va birinchi postingizni saqlang.",
    ru: "Выберите изображение товара и сохраните первый пост.",
  },
  "Start with an existing product. Prepare images and source text here.": {
    uz: "Mavjud mahsulotdan boshlang. Rasmlar va manba matnini shu yerda tayyorlang.",
    ru: "Начните с существующего товара. Подготовьте изображения и исходный текст здесь.",
  },
  "A new look or background suggestion for an existing product.": {
    uz: "Mavjud mahsulotdan yangi ko‘rinish yoki fon taklifi.",
    ru: "Предложение нового образа или фона для существующего товара.",
  },
  "Existing plan (optional)": {
    uz: "Mavjud reja (ixtiyoriy)",
    ru: "Существующий план (необязательно)",
  },
  Themes: {
    uz: "Mavzular",
    ru: "Темы",
  },
  "Themes / content directions": {
    uz: "Mavzular / kontent yo‘nalishlari",
    ru: "Темы / направления контента",
  },
  "In navigation": {
    uz: "Menyuda",
    ru: "В меню",
  },
  "In navigation (": {
    uz: "Menyuda (",
    ru: "В меню (",
  },
  "Meta description": {
    uz: "Meta tavsif",
    ru: "Метаописание",
  },
  "Mobile sections": {
    uz: "Mobil bo‘limlar",
    ru: "Разделы на мобильном",
  },
  "Model unavailable": {
    uz: "Model mavjud emas",
    ru: "Модель недоступна",
  },
  "Model setting": {
    uz: "Model sozlamasi",
    ru: "Настройка модели",
  },
  Monitoring: {
    uz: "Monitoring",
    ru: "Мониторинг",
  },
  "No matching product found.": {
    uz: "Mos mahsulot topilmadi.",
    ru: "Подходящий товар не найден.",
  },
  "Matching fabric": {
    uz: "Mos mato",
    ru: "Подходящая ткань",
  },
  "Matching fabric photo": {
    uz: "Mos mato fotosi",
    ru: "Фото подходящей ткани",
  },
  "Matching fabric on the right half, with the product over it.": {
    uz: "Mos mato fotosi o‘ng yarmida, mahsulot uning ustida.",
    ru: "Подходящая ткань в правой половине, товар поверх неё.",
  },
  "Upload a matching fabric photo in Products.": {
    uz: "Mos mato fotosini Mahsulotlar bo‘limiga yuklang.",
    ru: "Загрузите фото подходящей ткани в разделе товаров.",
  },
  "Upload a matching fabric photo and place the product over it.": {
    uz: "Mos mato fotosini yuklab, mahsulotni ustiga joylashtiring.",
    ru: "Загрузите фото подходящей ткани и разместите товар поверх него.",
  },
  "No matching post found": {
    uz: "Mos post topilmadi",
    ru: "Подходящий пост не найден",
  },
  Cover: {
    uz: "Muqova",
    ru: "Обложка",
  },
  "Cover caption": {
    uz: "Muqova izohi",
    ru: "Подпись к обложке",
  },
  "Cover description": {
    uz: "Muqova tavsifi",
    ru: "Описание обложки",
  },
  "cover description and caption": {
    uz: "muqova tavsifi va izohi",
    ru: "описание и подпись обложки",
  },
  "Cover, products and related stories": {
    uz: "Muqova, mahsulotlar va bog‘liq hikoyalar",
    ru: "Обложка, товары и связанные статьи",
  },
  "Remove cover": {
    uz: "Muqovani olib tashlash",
    ru: "Удалить обложку",
  },
  "Successful check:": {
    uz: "Muvaffaqiyatli tekshiruv:",
    ru: "Успешная проверка:",
  },
  "Name, contact, piece, owner or note": {
    uz: "Ism, aloqa, mahsulot, mas’ul yoki izoh",
    ru: "Имя, контакт, изделие, ответственный или заметка",
  },
  "Price not set": {
    uz: "Narx kiritilmagan",
    ru: "Цена не указана",
  },
  Publication: {
    uz: "Nashr",
    ru: "Публикация",
  },
  "Save changes before publishing actions.": {
    uz: "Nashr amallari oldidan o‘zgarishlarni saqlang.",
    ru: "Сохраните изменения перед действиями с публикацией.",
  },
  "Publication note": {
    uz: "Nashr izohi",
    ru: "Примечание к публикации",
  },
  "Add before publishing:": {
    uz: "Nashrdan oldin qo‘shing:",
    ru: "Добавьте перед публикацией:",
  },
  "Outcome unknown": {
    uz: "Natija noma'lum",
    ru: "Результат неизвестен",
  },
  "The result will appear here.": {
    uz: "Natija shu yerda ko‘rinadi.",
    ru: "Результат появится здесь.",
  },
  Results: {
    uz: "Natijalar",
    ru: "Результаты",
  },
  "Results history": {
    uz: "Natijalar tarixi",
    ru: "История результатов",
  },
  "Results check interval (days)": {
    uz: "Natijalarni tekshirish (kun)",
    ru: "Интервал проверки результатов (дни)",
  },
  Queued: {
    uz: "Navbatda",
    ru: "В очереди",
  },
  "Cancel queue item": {
    uz: "Navbatni bekor qilish",
    ru: "Отменить отправку в очереди",
  },
  "Days to schedule ahead (0 — off)": {
    uz: "Necha kun oldinga rejalashtirish (0 — o‘chiq)",
    ru: "Планировать на дней вперёд (0 — выключено)",
  },
  new: {
    uz: "yangi",
    ru: "новое",
  },
  Next: {
    uz: "Keyingi",
    ru: "Далее",
  },
  "Next step": {
    uz: "Keyingi qadam",
    ru: "Следующий шаг",
  },
  "Next:": {
    uz: "Keyingi:",
    ru: "Далее:",
  },
  "What interested people?": {
    uz: "Nima odamlarni qiziqtirdi?",
    ru: "Что заинтересовало людей?",
  },
  "What needs attention?": {
    uz: "Nimani e’tiborga olish kerak?",
    ru: "На что обратить внимание?",
  },
  "No enquiries in this view.": {
    uz: "Bu ko‘rinishda so‘rovlar yo‘q.",
    ru: "В этом представлении обращений нет.",
  },
  "No recorded changes yet. Earlier notes remain on the enquiry.": {
    uz: "Hozircha o‘zgarishlar qayd etilmagan. Oldingi izohlar so‘rovda qoladi.",
    ru: "Изменений пока нет. Предыдущие заметки остаются в обращении.",
  },
  Unknown: {
    uz: "Noma’lum",
    ru: "Неизвестно",
  },
  Name: {
    uz: "Nomi",
    ru: "Название",
  },
  "Name (": {
    uz: "Nomi (",
    ru: "Название (",
  },
  off: {
    uz: "o‘chiq",
    ru: "выкл.",
  },
  Disabled: {
    uz: "O‘chirilgan",
    ru: "Отключено",
  },
  Delete: {
    uz: "O‘chirish",
    ru: "Удалить",
  },
  "Size, fabric and purchase details.": {
    uz: "O‘lcham, mato va xarid ma’lumotlari.",
    ru: "Размер, ткань и информация о покупке.",
  },
  Read: {
    uz: "O‘qish",
    ru: "Читать",
  },
  "Reader promise (internal, not published)": {
    uz: "O‘quvchiga va’da (ichki, chop etilmaydi)",
    ru: "Обещание читателю (внутреннее, не публикуется)",
  },
  "Change note": {
    uz: "O‘zgarish izohi",
    ru: "Примечание к изменению",
  },
  "Change note (optional)": {
    uz: "O‘zgarish izohi (ixtiyoriy)",
    ru: "Примечание к изменению (необязательно)",
  },
  "Repeating an unchanged request does not create a new paid analysis. Changing the source or prompt starts a new request.":
    {
      uz: "O‘zgarmagan so‘rov qayta bosilsa yangi pulli tahlil yaratilmaydi. Manba yoki izohni o‘zgartirish yangi niyatni boshlaydi.",
      ru: "Повтор неизменённого запроса не создаёт новый платный анализ. Изменение источника или задания начинает новый запрос.",
    },
  Followers: {
    uz: "Obunachilar",
    ru: "Подписчики",
  },
  "A post draft announcing a published Ocharo Blog story.": {
    uz: "Ocharo Blogdagi nashr qilingan hikoyani e’lon qiluvchi post qoralamasi.",
    ru: "Черновик поста об опубликованной статье Ocharo Blog.",
  },
  "Ocharo enquiries": {
    uz: "Ocharo so‘rovlari",
    ru: "Обращения Ocharo",
  },
  "Ocharo Catalogue": {
    uz: "Ocharo Katalog",
    ru: "Каталог Ocharo",
  },
  "Refresh from Ocharo Catalogue": {
    uz: "Ocharo Katalogdan yangilash",
    ru: "Обновить из каталога Ocharo",
  },
  "Ocharo platform": {
    uz: "Ocharo platformasi",
    ru: "Платформа Ocharo",
  },
  of: {
    uz: "jami",
    ru: "из",
  },
  "Front view": {
    uz: "Old ko‘rinish",
    ru: "Вид спереди",
  },
  "Start with the front view.": {
    uz: "Old ko‘rinishdan boshlang.",
    ru: "Начните с вида спереди.",
  },
  "Choose the front view, apply a template and prepare your post.": {
    uz: "Old ko‘rinishni tanlang, shablonni qo‘llang va postingizni tayyorlang.",
    ru: "Выберите вид спереди, примените шаблон и подготовьте пост.",
  },
  "Choose front view": {
    uz: "Old ko‘rinishni tanlash",
    ru: "Выбрать вид спереди",
  },
  "Previous week": {
    uz: "Oldingi hafta",
    ru: "Предыдущая неделя",
  },
  "Restore previous text": {
    uz: "Oldingi matnni tiklash",
    ru: "Восстановить предыдущий текст",
  },
  "received ·": {
    uz: "olindi ·",
    ru: "получено ·",
  },
  "Open Connected apps in Platform, choose this desk, then return here. Unsaved edits stay open.":
    {
      uz: "Platform’da Ulangan ilovalarni oching, ushbu ish stolini tanlang va qayting. Saqlanmagan tahrirlar ochiq qoladi.",
      ru: "Откройте подключённые приложения в Platform, выберите этот кабинет и вернитесь. Несохранённые изменения останутся открытыми.",
    },
  "Open imported draft →": {
    uz: "Import qilingan qoralamani ochish →",
    ru: "Открыть импортированный черновик →",
  },
  "Open Platform ↗": {
    uz: "Platform’ni ochish ↗",
    ru: "Открыть Platform ↗",
  },
  "OpenAI · This button may start paid image analysis. Results are not accepted automatically.":
    {
      uz: "OpenAI · Bu tugma pulli tasvir tahlilini boshlashi mumkin. Natija avtomatik qabul qilinmaydi.",
      ru: "OpenAI · Кнопка может запустить платный анализ изображения. Результаты не принимаются автоматически.",
    },
  "OpenAI is not configured. Analysis is unavailable. Refreshing status reads saved settings only.":
    {
      uz: "OpenAI hozir sozlanmagan. Tahlil mavjud emas. Holatni yangilash faqat saqlangan sozlamalarni o‘qiydi.",
      ru: "OpenAI не настроен. Анализ недоступен. Обновление статуса читает только сохранённые настройки.",
    },
  "Optional. Clear this date before closing.": {
    uz: "Ixtiyoriy. Yopishdan oldin bu sanani tozalang.",
    ru: "Необязательно. Удалите эту дату перед закрытием.",
  },
  "Last catalogue refresh:": {
    uz: "Oxirgi katalog yangilanishi:",
    ru: "Последнее обновление каталога:",
  },
  "Last request:": {
    uz: "Oxirgi so‘rov:",
    ru: "Последний запрос:",
  },
  "Last check failed": {
    uz: "Oxirgi tekshiruv xato",
    ru: "Последняя проверка не прошла",
  },
  "Last error (": {
    uz: "Oxirgi xato (",
    ru: "Последняя ошибка (",
  },
  Thu: {
    uz: "Pay",
    ru: "Чт",
  },
  "Paragraphs (separate with blank lines)": {
    uz: "Paragraflar (bo‘sh qator bilan ajrating)",
    ru: "Абзацы (разделяйте пустой строкой)",
  },
  "Move down": {
    uz: "Pastga",
    ru: "Вниз",
  },
  "Footer links": {
    uz: "Pastki havolalar",
    ru: "Ссылки в подвале",
  },
  "Bottom line": {
    uz: "Pastki qator",
    ru: "Нижняя строка",
  },
  "The PDF uses the exact selected PNG images. Captions stay outside the cards.":
    {
      uz: "PDF postlarning aynan tanlangan PNG tasvirlarini ishlatadi. Izohlar kartadan tashqarida qoladi.",
      ru: "PDF использует именно выбранные PNG-изображения. Подписи остаются за пределами карточек.",
    },
  "Platform connected": {
    uz: "Platform ulangan",
    ru: "Platform подключена",
  },
  "Platform connection": {
    uz: "Platform ulanishi",
    ru: "Подключение к Platform",
  },
  Post: {
    uz: "Post",
    ru: "Пост",
  },
  "Post preview": {
    uz: "Post ko‘rinishi",
    ru: "Предпросмотр поста",
  },
  "Post caption": {
    uz: "Post matni",
    ru: "Текст поста",
  },
  "Add to post caption": {
    uz: "Post matniga qo‘shish",
    ru: "Добавить в текст поста",
  },
  "Post name": {
    uz: "Post nomi",
    ru: "Название поста",
  },
  "Post draft": {
    uz: "Post qoralamasi",
    ru: "Черновик поста",
  },
  "Post added to the plan.": {
    uz: "Post rejaga qo‘shildi.",
    ru: "Пост добавлен в план.",
  },
  "Post saved.": {
    uz: "Post saqlandi.",
    ru: "Пост сохранён.",
  },
  "Post title": {
    uz: "Post sarlavhasi",
    ru: "Заголовок поста",
  },
  "The original source for this post image was not found.": {
    uz: "Post tasvirining asl manbasi topilmadi.",
    ru: "Исходное изображение для этого поста не найдено.",
  },
  "Post language": {
    uz: "Post tili",
    ru: "Язык поста",
  },
  "Post package downloaded.": {
    uz: "Post to‘plami yuklab olindi.",
    ru: "Пакет поста скачан.",
  },
  "Post not found": {
    uz: "Post topilmadi",
    ru: "Пост не найден",
  },
  "Posts and automation": {
    uz: "Post va avtomatika",
    ru: "Посты и автоматизация",
  },
  "Create post": {
    uz: "Post yaratish",
    ru: "Создать пост",
  },
  "Cards prepared in the post composer will appear here.": {
    uz: "Post yaratishda tayyorlangan kartalar shu yerda paydo bo‘ladi.",
    ru: "Здесь появятся карточки, подготовленные в редакторе постов.",
  },
  "The post, destination or plan changed. Review the updated state.": {
    uz: "Post, manzil yoki reja o‘zgardi. Yangilangan holatni qayta ko‘rib chiqing.",
    ru: "Пост, адрес или план изменились. Проверьте обновлённое состояние.",
  },
  "Use in post": {
    uz: "Postda ishlatish",
    ru: "Использовать в посте",
  },
  "Apply to post": {
    uz: "Postda qo‘llash",
    ru: "Применить к посту",
  },
  "Image added to post": {
    uz: "Postga qo‘shilgan tasvir",
    ru: "Изображение в посте",
  },
  "Add to post": {
    uz: "Postga qo‘shish",
    ru: "Добавить в пост",
  },
  "Give your post its shape.": {
    uz: "Postingizga shakl bering.",
    ru: "Придайте посту форму.",
  },
  Posts: {
    uz: "Postlar",
    ru: "Посты",
  },
  "Post flow": {
    uz: "Postlar oqimi",
    ru: "Поток постов",
  },
  "Where did posts stop?": {
    uz: "Postlar qayerda to‘xtadi?",
    ru: "Где остановились посты?",
  },
  "Open an existing variant from the post list.": {
    uz: "Postlar ro‘yxatidan mavjud variantni oching.",
    ru: "Откройте существующий вариант из списка постов.",
  },
  "Post calendar": {
    uz: "Postlar taqvimi",
    ru: "Календарь постов",
  },
  "Back to posts": {
    uz: "Postlarga qaytish",
    ru: "К постам",
  },
  "Open post": {
    uz: "Postni ochish",
    ru: "Открыть пост",
  },
  "Schedule post": {
    uz: "Postni rejalashtirish",
    ru: "Запланировать пост",
  },
  "Edit post": {
    uz: "Postni tahrirlash",
    ru: "Редактировать пост",
  },
  "Choose a post": {
    uz: "Postni tanlash",
    ru: "Выберите пост",
  },
  "Prepare post": {
    uz: "Postni tayyorlash",
    ru: "Подготовить пост",
  },
  "Find a post": {
    uz: "Postni topish",
    ru: "Найти пост",
  },
  "Find a post…": {
    uz: "Postni topish…",
    ru: "Найти пост…",
  },
  "Cumulative post metrics": {
    uz: "Postning jamlangan ko‘rsatkichlari",
    ru: "Накопленные метрики поста",
  },
  "Preview Atlas ↗": {
    uz: "Atlas’ni ko‘rish ↗",
    ru: "Предпросмотр Atlas ↗",
  },
  "Preview Wear ↗": {
    uz: "Wear’ni ko‘rish ↗",
    ru: "Предпросмотр Wear ↗",
  },
  Previous: {
    uz: "Oldingi",
    ru: "Назад",
  },
  "Product context": {
    uz: "Mahsulot konteksti",
    ru: "Контекст товара",
  },
  "Publication & provenance": {
    uz: "Nashr va kelib chiqish",
    ru: "Публикация и происхождение",
  },
  Reach: {
    uz: "Qamrov",
    ru: "Охват",
  },
  "The difference in brackets compares the previous manual observation of the same URL. Channel API counts are not added.":
    {
      uz: "Qavsdagi farq: shu havolaning oldingi qo‘lda kuzatuviga nisbatan. Kanal API sonlari qo‘shilmaydi.",
      ru: "Разница в скобках — относительно предыдущего ручного наблюдения той же ссылки. Значения API канала не суммируются.",
    },
  "Supporting which claim (optional)": {
    uz: "Qaysi fikrni tasdiqlaydi (ixtiyoriy)",
    ru: "Какой тезис подтверждает (необязательно)",
  },
  "Review again": {
    uz: "Qayta ko‘rib chiqish",
    ru: "Проверить повторно",
  },
  "Needs rechecking": {
    uz: "Qayta tekshirish kerak",
    ru: "Нужна повторная проверка",
  },
  Retry: {
    uz: "Qayta urinish",
    ru: "Повторить",
  },
  Search: {
    uz: "Qidirish",
    ru: "Поиск",
  },
  "Hide from search engines (noindex)": {
    uz: "Qidiruv tizimlaridan yashirish (noindex)",
    ru: "Скрыть от поисковиков (noindex)",
  },
  Partial: {
    uz: "Qisman olingan",
    ru: "Частично",
  },
  "short description": {
    uz: "qisqa tavsif",
    ru: "краткое описание",
  },
  "Short description (card and feed)": {
    uz: "Qisqa tavsif (kartochka va feed)",
    ru: "Краткое описание (карточка и лента)",
  },
  "Manually observed results": {
    uz: "Qo‘lda kuzatilgan natijalar",
    ru: "Результаты ручных наблюдений",
  },
  "Manual observation saved. Channel confirmation is unchanged.": {
    uz: "Qo‘lda kuzatuv saqlandi. Kanal tasdig‘i o‘zgarmadi.",
    ru: "Ручное наблюдение сохранено. Подтверждение канала не изменено.",
  },
  "Manually recorded": {
    uz: "Qo‘lda qayd etilgan",
    ru: "Записано вручную",
  },
  Unsupported: {
    uz: "Qo'llanmaydi",
    ru: "Не поддерживается",
  },
  Apply: {
    uz: "Qo‘llash",
    ru: "Применить",
  },
  "Additional line": {
    uz: "Qo‘shimcha satr",
    ru: "Дополнительная строка",
  },
  Add: {
    uz: "Qo‘shish",
    ru: "Добавить",
  },
  "Dark background": {
    uz: "Qora fon",
    ru: "Тёмный фон",
  },
  Draft: {
    uz: "Qoralama",
    ru: "Черновик",
  },
  "Draft exists": {
    uz: "Qoralama bor",
    ru: "Есть черновик",
  },
  "The draft was updated. Open the current revision.": {
    uz: "Qoralama yangilangan. Hozirgi talqinni ochish.",
    ru: "Черновик обновлён. Откройте текущую версию.",
  },
  "drafts created": {
    uz: "qoralama yaratildi",
    ru: "черновиков создано",
  },
  "Move to draft": {
    uz: "Qoralamaga",
    ru: "В черновик",
  },
  Drafts: {
    uz: "Qoralamalar",
    ru: "Черновики",
  },
  qualified: {
    uz: "saralangan",
    ru: "квалифицировано",
  },
  "Where did the image come from?": {
    uz: "Rasm qayerdan olingan?",
    ru: "Откуда взято изображение?",
  },
  "images saved": {
    uz: "rasm saqlandi",
    ru: "изображений сохранено",
  },
  "Image description (alt)": {
    uz: "Rasm tavsifi (alt)",
    ru: "Описание изображения (alt)",
  },
  "Product for image": {
    uz: "Rasm uchun mahsulot",
    ru: "Товар для изображения",
  },
  "Upload image": {
    uz: "Rasm yuklash",
    ru: "Загрузить изображение",
  },
  "Image:": {
    uz: "Rasm:",
    ru: "Изображение:",
  },
  "Image: first product": {
    uz: "Rasm: birinchi mahsulot",
    ru: "Изображение: первый товар",
  },
  "Official documentation": {
    uz: "Rasmiy hujjat",
    ru: "Официальная документация",
  },
  "Keep enabled until the official launch. Turning it off allows indexing in robots.txt and pages.":
    {
      uz: "Rasmiy ochilishgacha yoqilgan qoladi. O‘chirsangiz, robots.txt va sahifalar indekslashga ruxsat beradi.",
      ru: "Оставьте включённым до официального запуска. При отключении robots.txt и страницы разрешат индексацию.",
    },
  "Official setup guide": {
    uz: "Rasmiy sozlash yo‘riqnomasi",
    ru: "Официальная инструкция по настройке",
  },
  Images: {
    uz: "Tasvirlar",
    ru: "Изображения",
  },
  "Read access": {
    uz: "O‘qish huquqi",
    ru: "Доступ к чтению",
  },
  "Reading service version…": {
    uz: "Xizmat versiyasi o‘qilmoqda…",
    ru: "Получение версии сервиса…",
  },
  "Record uncertainty in the attribution note. Every save retains this metadata in its revision.":
    {
      uz: "Noaniqlikni kelib chiqish izohida qayd eting. Har saqlashda bu ma’lumotlar talqinda saqlanadi.",
      ru: "Укажите неопределённость в примечании об атрибуции. Каждое сохранение включает эти данные в версию.",
    },
  "Recorded by": {
    uz: "Qayd etgan",
    ru: "Кто записал",
  },
  Refresh: {
    uz: "Yangilash",
    ru: "Обновить",
  },
  "Plan cancelled.": {
    uz: "Reja bekor qilindi.",
    ru: "План отменён.",
  },
  "No plan set": {
    uz: "Reja belgilanmagan",
    ru: "План не задан",
  },
  "The planned time must be in the future.": {
    uz: "Reja vaqti kelajakda bo‘lishi kerak.",
    ru: "Запланированное время должно быть в будущем.",
  },
  "plans. Results:": {
    uz: "reja. Natijalar:",
    ru: "планов. Результаты:",
  },
  Planned: {
    uz: "Rejada",
    ru: "Запланировано",
  },
  "Active in plan": {
    uz: "Rejada faol",
    ru: "Активен в плане",
  },
  "Planned:": {
    uz: "Rejada:",
    ru: "В плане:",
  },
  "The planned revision no longer matches the reviewed post. Create a new plan.":
    {
      uz: "Rejadagi talqin hozirgi tekshirilgan post bilan mos emas. Yangi reja yarating.",
      ru: "Запланированная версия больше не совпадает с проверенным постом. Создайте новый план.",
    },
  "To plan": {
    uz: "Rejaga",
    ru: "В план",
  },
  "Not linked to a plan": {
    uz: "Rejaga bog‘lanmagan",
    ru: "Не связано с планом",
  },
  "Add to plan": {
    uz: "Rejaga qo‘shish",
    ru: "Добавить в план",
  },
  Scheduled: {
    uz: "Rejalashtirilgan",
    ru: "Запланировано",
  },
  Scheduling: {
    uz: "Rejalashtirish",
    ru: "Планирование",
  },
  "Schedule (local time)": {
    uz: "Rejalashtirish (mahalliy vaqt)",
    ru: "Расписание (местное время)",
  },
  "Scheduling channel": {
    uz: "Rejalashtirish kanali",
    ru: "Канал планирования",
  },
  "Your plan is still empty": {
    uz: "Rejangiz hali bo‘sh",
    ru: "Ваш план пока пуст",
  },
  "View plan": {
    uz: "Rejani ko‘rish",
    ru: "Посмотреть план",
  },
  "Open plan": {
    uz: "Rejani ochish",
    ru: "Открыть план",
  },
  "Save plan": {
    uz: "Rejani saqlash",
    ru: "Сохранить план",
  },
  "Edit plan": {
    uz: "Rejani tahrirlash",
    ru: "Редактировать план",
  },
  plan: {
    uz: "rejasi",
    ru: "план",
  },
  "Retry image tools": {
    uz: "Tasvir vositalarini qayta yuklash",
    ru: "Повторить загрузку инструментов",
  },
  "Review the image dimensions and available tools before applying edits.": {
    uz: "Tahrirni qo‘llashdan oldin tasvir o‘lchamlari va mavjud vositalarni tekshiring.",
    ru: "Перед применением изменений проверьте размеры изображения и доступные инструменты.",
  },
  Revision: {
    uz: "Talqin",
    ru: "Версия",
  },
  "List (one item per line, optional)": {
    uz: "Ro‘yxat (har qatorda bittadan, ixtiyoriy)",
    ru: "Список (один пункт в строке, необязательно)",
  },
  Russian: {
    uz: "Ruscha",
    ru: "Русский",
  },
  "Authorization revoked": {
    uz: "Ruxsat bekor qilingan",
    ru: "Разрешение отозвано",
  },
  "Page not found": {
    uz: "Sahifa topilmadi",
    ru: "Страница не найдена",
  },
  "Page not found.": {
    uz: "Sahifa topilmadi.",
    ru: "Страница не найдена.",
  },
  "Date needs checking": {
    uz: "Sana tekshirilishi kerak",
    ru: "Нужно проверить дату",
  },
  "Date and time": {
    uz: "Sana va vaqt",
    ru: "Дата и время",
  },
  "Choose a date and time": {
    uz: "Sana va vaqtni tanlang",
    ru: "Выберите дату и время",
  },
  "Choose a date and time.": {
    uz: "Sana va vaqtni tanlang.",
    ru: "Выберите дату и время.",
  },
  "Check the date and time.": {
    uz: "Sana va vaqtni tekshiring.",
    ru: "Проверьте дату и время.",
  },
  "Fill in the date and time.": {
    uz: "Sana va vaqtni to‘ldiring.",
    ru: "Укажите дату и время.",
  },
  "Leave without saving": {
    uz: "Saqlamasdan chiqish",
    ru: "Выйти без сохранения",
  },
  "Close without saving": {
    uz: "Saqlamasdan yopish",
    ru: "Закрыть без сохранения",
  },
  "Saved state:": {
    uz: "Saqlangan holat:",
    ru: "Сохранённое состояние:",
  },
  "Refresh saved state": {
    uz: "Saqlangan holatni yangilash",
    ru: "Обновить сохранённое состояние",
  },
  "Saved channel confirmation was checked. The post was not resent.": {
    uz: "Saqlangan kanal tasdig‘i tekshirildi. Post qayta yuborilmadi.",
    ru: "Сохранённое подтверждение канала проверено. Пост повторно не отправлялся.",
  },
  "No saved observations. This does not mean zero results.": {
    uz: "Saqlangan kuzatuv yo‘q. Bu nol natija degani emas.",
    ru: "Сохранённых наблюдений нет. Это не означает нулевые результаты.",
  },
  "Saved observations": {
    uz: "Saqlangan kuzatuvlar",
    ru: "Сохранённые наблюдения",
  },
  "Loading saved observations…": {
    uz: "Saqlangan kuzatuvlar yuklanmoqda…",
    ru: "Загрузка сохранённых наблюдений…",
  },
  "Last saved error (": {
    uz: "Saqlangan oxirgi xato (",
    ru: "Последняя сохранённая ошибка (",
  },
  "Saved post": {
    uz: "Saqlangan post",
    ru: "Сохранённый пост",
  },
  "Open your saved post and review its text and appearance.": {
    uz: "Saqlangan postingizni ochib, matn va ko‘rinishini tekshiring.",
    ru: "Откройте сохранённый пост и проверьте текст и оформление.",
  },
  "Saved history": {
    uz: "Saqlangan tarix",
    ru: "Сохранённая история",
  },
  "Saved revision preview": {
    uz: "Saqlangan versiya ko‘rinishi",
    ru: "Предпросмотр сохранённой версии",
  },
  "Saved delivery revision": {
    uz: "Saqlangan yuborish talqini",
    ru: "Сохранённая версия отправки",
  },
  Unsaved: {
    uz: "Saqlanmagan",
    ru: "Не сохранено",
  },
  "Unsaved changes": {
    uz: "Saqlanmagan o‘zgarishlar",
    ru: "Несохранённые изменения",
  },
  "You have unsaved changes. Leave anyway?": {
    uz: "Saqlanmagan o‘zgarishlar bor. Chiqib ketilsinmi?",
    ru: "Есть несохранённые изменения. Всё равно выйти?",
  },
  Save: {
    uz: "Saqlash",
    ru: "Сохранить",
  },
  "Track saves and questions about the product.": {
    uz: "Saqlash va mahsulot haqidagi savollarni kuzating.",
    ru: "Следите за сохранениями и вопросами о товаре.",
  },
  Saves: {
    uz: "Saqlashlar",
    ru: "Сохранения",
  },
  Title: {
    uz: "Sarlavha",
    ru: "Заголовок",
  },
  "Title, URL or description": {
    uz: "Sarlavha, manzil yoki tavsif",
    ru: "Заголовок, адрес или описание",
  },
  "Save update": {
    uz: "O‘zgarishni saqlash",
    ru: "Сохранить изменение",
  },
  "Saving…": {
    uz: "Saqlanmoqda…",
    ru: "Сохранение…",
  },
  Site: {
    uz: "Sayt",
    ru: "Сайт",
  },
  "Site navigation and /topics/… pages.": {
    uz: "Sayt menyusi va /topics/… sahifalari.",
    ru: "Меню сайта и страницы /topics/…",
  },
  "Site name": {
    uz: "Sayt nomi",
    ru: "Название сайта",
  },
  "Site settings saved.": {
    uz: "Sayt sozlamalari saqlandi.",
    ru: "Настройки сайта сохранены.",
  },
  "open on site": {
    uz: "saytda ochish",
    ru: "открыть на сайте",
  },
  "Open site": {
    uz: "Saytni ochish",
    ru: "Открыть сайт",
  },
  Tue: {
    uz: "Sesh",
    ru: "Вт",
  },
  "Search enquiries": {
    uz: "So‘rovlarni qidirish",
    ru: "Поиск обращений",
  },
  "SEO title": {
    uz: "SEO sarlavha",
    ru: "SEO-заголовок",
  },
  "SEO title (optional)": {
    uz: "SEO sarlavha (ixtiyoriy)",
    ru: "SEO-заголовок (необязательно)",
  },
  "SEO description": {
    uz: "SEO tavsif",
    ru: "SEO-описание",
  },
  "SEO description (optional)": {
    uz: "SEO tavsif (ixtiyoriy)",
    ru: "SEO-описание (необязательно)",
  },
  "The server checks every 5 minutes. It refreshes the catalogue and schedules approved posts into available slots; it does not send anything automatically.":
    {
      uz: "Server har 5 daqiqada tekshiradi. Katalogni yangilaydi va tasdiqlangan postlarni bo‘sh vaqtlarga rejalashtiradi — hech narsani o‘zi yubormaydi.",
      ru: "Сервер проверяет каждые 5 минут: обновляет каталог и размещает проверенные посты в свободных слотах. Самостоятельно ничего не отправляет.",
    },
  "Unexpected server response. Try again.": {
    uz: "Server javobi kutilgan formatda emas. Qayta urinib ko'ring.",
    ru: "Неожиданный формат ответа сервера. Попробуйте снова.",
  },
  "Unable to connect to the server. Check the local service is running.": {
    uz: "Serverga ulanib bo'lmadi. Mahalliy xizmat ishlayotganini tekshiring.",
    ru: "Не удалось подключиться к серверу. Проверьте, запущен ли локальный сервис.",
  },
  "Service version unavailable. Check your connection.": {
    uz: "Xizmat versiyasi mavjud emas. Ulanishni tekshiring.",
    ru: "Версия сервиса недоступна. Проверьте подключение.",
  },
  "Service:": {
    uz: "Xizmat:",
    ru: "Сервис:",
  },
  Sat: {
    uz: "Shan",
    ru: "Сб",
  },
  "Choose a template": {
    uz: "Shablon tanlang",
    ru: "Выберите шаблон",
  },
  "Select template": {
    uz: "Shablon tanlash",
    ru: "Выбор шаблона",
  },
  "Template and text review are part of post creation.": {
    uz: "Shablon va matnni tekshirish post yaratishning bir qismidir.",
    ru: "Выбор шаблона и проверка текста — часть создания поста.",
  },
  "Choose templates together": {
    uz: "Shablonlarni birga tanlang",
    ru: "Выберите шаблоны вместе",
  },
  "Compare templates here. Apply the chosen variant to an everyday post.": {
    uz: "Shablonni shu yerda taqqoslang. Kundalik post uchun tanlangan variantni qo‘llang.",
    ru: "Сравните шаблоны здесь. Примените выбранный вариант к обычному посту.",
  },
  "Steady, without rushing.": {
    uz: "Shoshilmasdan, muntazam.",
    ru: "Регулярно, без спешки.",
  },
  "This week": {
    uz: "Shu hafta",
    ru: "Эта неделя",
  },
  "Start with this product": {
    uz: "Shu mahsulotdan boshlash",
    ru: "Начать с этого товара",
  },
  "I authorize sending this post and its images to the shown account at the scheduled time.":
    {
      uz: "Shu post va tasvirlarni ko‘rsatilgan akkauntga belgilangan vaqtda yuborishga ruxsat beraman.",
      ru: "Разрешаю отправить этот пост и изображения в указанный аккаунт в назначенное время.",
    },
  "This post variant was reviewed.": {
    uz: "Shu post varianti tekshirildi.",
    ru: "Этот вариант поста проверен.",
  },
  "Observation history for this post": {
    uz: "Shu postning kuzatuv tarixi",
    ru: "История наблюдений этого поста",
  },
  "This variant was reviewed": {
    uz: "Shu variant tekshirildi",
    ru: "Этот вариант проверен",
  },
  "Signal:": {
    uz: "Signal:",
    ru: "Сигнал:",
  },
  "Your reviewed text": {
    uz: "Siz tekshirgan matn",
    ru: "Проверенный вами текст",
  },
  Slot: {
    uz: "Slot",
    ru: "Слот",
  },
  "Delete slot": {
    uz: "Slotni o‘chirish",
    ru: "Удалить слот",
  },
  words: {
    uz: "so‘z",
    ru: "слов",
  },
  "words ·": {
    uz: "so‘z ·",
    ru: "слов ·",
  },
  Configured: {
    uz: "Sozlangan",
    ru: "Настроено",
  },
  "Configured service does not mean a verified connection. Secret keys are not entered on this page.":
    {
      uz: "Sozlangan xizmat hali tekshirilgan ulanish degani emas. Maxfiy kalitlar bu sahifada kiritilmaydi.",
      ru: "Настроенный сервис не означает проверенное подключение. Секретные ключи на этой странице не вводятся.",
    },
  "Static site (ZIP)": {
    uz: "Statik sayt (ZIP)",
    ru: "Статический сайт (ZIP)",
  },
  Studio: {
    uz: "Studio",
    ru: "Студия",
  },
  "Studio imports": {
    uz: "Studio importlari",
    ru: "Импорт из Studio",
  },
  "Studio review JSON": {
    uz: "Studio tekshiruv JSON’i",
    ru: "JSON проверки Studio",
  },
  "stories. Published stories appear on the site, in RSS and the sitemap.": {
    uz: "ta hikoya. Nashr qilinganlari saytda, RSS va sitemap’da.",
    ru: "статей. Опубликованные статьи доступны на сайте, в RSS и sitemap.",
  },
  "products in the workspace. Active catalogue products, prices and text in three languages appear here.":
    {
      uz: "ta mahsulot ish stolida. Katalogdagi faol mahsulotlar, narxlar va uch tildagi matn shu yerga keladi.",
      ru: "товаров в кабинете. Здесь активные товары каталога, цены и тексты на трёх языках.",
    },
  "drafts to create": {
    uz: "ta qoralama yaratish",
    ru: "черновиков создать",
  },
  "approved posts have no plan.": {
    uz: "ta tasdiqlangan post rejasiz turibdi.",
    ru: "проверенных постов без плана.",
  },
  "new plans": {
    uz: "ta yangi reja",
    ru: "новых планов",
  },
  Analysis: {
    uz: "Tahlil",
    ru: "Анализ",
  },
  "Analysis language": {
    uz: "Tahlil tili",
    ru: "Язык анализа",
  },
  "Changes are unsaved. Stay on this page or leave without saving.": {
    uz: "Tahrir saqlanmagan. Sahifada qoling yoki saqlamasdan chiqing.",
    ru: "Изменения не сохранены. Останьтесь на странице или выйдите без сохранения.",
  },
  "create edition": {
    uz: "tahririni yaratish",
    ru: "создать версию",
  },
  "Continue editing": {
    uz: "Tahrirlashni davom ettirish",
    ru: "Продолжить редактирование",
  },
  "Discard changes": {
    uz: "Tahrirni bekor qilish",
    ru: "Отменить изменения",
  },
  Suggestions: {
    uz: "Takliflar",
    ru: "Предложения",
  },
  "Suggestions do not change product facts automatically.": {
    uz: "Takliflar mahsulot ma’lumotlarini o‘z-o‘zidan o‘zgartirmaydi.",
    ru: "Предложения не изменяют данные товара автоматически.",
  },
  "Review suggestion": {
    uz: "Taklifni ko‘rib chiqish",
    ru: "Проверить предложение",
  },
  "Selected images must total no more than 12 MiB.": {
    uz: "Tanlangan tasvirlar jami 12 MiB dan oshmasin.",
    ru: "Общий размер выбранных изображений не должен превышать 12 МиБ.",
  },
  "Selected images will be sent to the service with the request.": {
    uz: "Tanlangan tasvirlar so‘rov bilan xizmatga yuboriladi.",
    ru: "Выбранные изображения будут отправлены сервису вместе с запросом.",
  },
  "Selecting a channel does not connect it.": {
    uz: "Tanlash kanalga ulanganlikni anglatmaydi.",
    ru: "Выбор канала не подключает его.",
  },
  Calendar: {
    uz: "Taqvim",
    ru: "Календарь",
  },
  "View in calendar": {
    uz: "Taqvimda ko‘rish",
    ru: "Открыть в календаре",
  },
  History: {
    uz: "Tarix",
    ru: "История",
  },
  "Translation draft created — translate the text into that language and publish it.":
    {
      uz: "Tarjima qoralamasi yaratildi — matnni shu tilga o‘giring va nashr qiling.",
      ru: "Создан черновик перевода — переведите текст на выбранный язык и опубликуйте.",
    },
  Order: {
    uz: "Tartib",
    ru: "Порядок",
  },
  "Confirmation:": {
    uz: "Tasdiq:",
    ru: "Подтверждение:",
  },
  Confirmed: {
    uz: "Tasdiqlangan",
    ru: "Подтверждено",
  },
  "Send through a verified connection or export manually": {
    uz: "Tasdiqlangan ulanish orqali yuborish yoki qo‘lda eksport",
    ru: "Отправить через проверенное подключение или экспортировать вручную",
  },
  "Approved, unplanned": {
    uz: "Tasdiqlangan, rejasiz",
    ru: "Проверено, без плана",
  },
  "External account verification is unavailable. Configuration does not authorize delivery.":
    {
      uz: "Tashqi akkaunt tekshiruvi hozir mavjud emas. Sozlanganlik yuborishga ruxsat bermaydi.",
      ru: "Проверка внешнего аккаунта недоступна. Настройка не разрешает отправку.",
    },
  "shown. Weekly totals use the full history.": {
    uz: "tasi ko‘rsatilgan. Haftalik hisob to‘liq tarixdan olinadi.",
    ru: "показано. Недельные итоги рассчитаны по полной истории.",
  },
  "This image exceeds the channel’s size limit.": {
    uz: "Tasvir bu kanalning hajm chegarasidan katta.",
    ru: "Изображение превышает ограничение размера канала.",
  },
  "Image placement": {
    uz: "Tasvir joylashuvi",
    ru: "Расположение изображения",
  },
  "Image added to the library.": {
    uz: "Tasvir kutubxonaga qo‘shildi.",
    ru: "Изображение добавлено в библиотеку.",
  },
  "Image source": {
    uz: "Tasvir manbasi",
    ru: "Источник изображения",
  },
  "Add image": {
    uz: "Tasvir qo‘shish",
    ru: "Добавить изображение",
  },
  "Image request queued.": {
    uz: "Tasvir so‘rovi navbatga qo‘shildi.",
    ru: "Запрос изображения добавлен в очередь.",
  },
  "Image analysis request": {
    uz: "Tasvir tahlili so‘rovi",
    ru: "Запрос анализа изображения",
  },
  "Image preparation": {
    uz: "Tasvir tayyorlash",
    ru: "Подготовка изображения",
  },
  "Image preparation queued.": {
    uz: "Tasvir tayyorlash navbatga qo‘shildi.",
    ru: "Подготовка изображения добавлена в очередь.",
  },
  "Image type": {
    uz: "Tasvir turi",
    ru: "Тип изображения",
  },
  "Image prompt": {
    uz: "Tasvir uchun topshiriq",
    ru: "Задание для изображения",
  },
  "Write an image prompt.": {
    uz: "Tasvir uchun topshiriq yozing.",
    ru: "Введите задание для изображения.",
  },
  "Generate image": {
    uz: "Tasvir yaratish",
    ru: "Создать изображение",
  },
  "Product for image generation": {
    uz: "Tasvir yaratish mahsuloti",
    ru: "Товар для генерации изображения",
  },
  "Image generation model": {
    uz: "Tasvir yaratish modeli",
    ru: "Модель генерации изображений",
  },
  "Image generation service": {
    uz: "Tasvir yaratish xizmati",
    ru: "Сервис генерации изображений",
  },
  "Post idea from an image": {
    uz: "Tasvirdan post g‘oyasi",
    ru: "Идея поста по изображению",
  },
  "Analyse images": {
    uz: "Tasvirlarni tahlil qilish",
    ru: "Анализировать изображения",
  },
  "Analyse image": {
    uz: "Tasvirni tahlil qilish",
    ru: "Анализировать изображение",
  },
  "Prepare the image and add it to the post.": {
    uz: "Tasvirni tayyorlab, postga qo‘shing.",
    ru: "Подготовьте изображение и добавьте его в пост.",
  },
  "Prepare image": {
    uz: "Tasvirni tayyorlash",
    ru: "Подготовить изображение",
  },
  Description: {
    uz: "Tavsif",
    ru: "Описание",
  },
  "Description (": {
    uz: "Tavsif (",
    ru: "Описание (",
  },
  Ready: {
    uz: "Tayyor",
    ru: "Готово",
  },
  "Prepared card": {
    uz: "Tayyor karta",
    ru: "Готовая карточка",
  },
  "Journal from prepared cards": {
    uz: "Tayyor kartalardan jurnal",
    ru: "Журнал из готовых карточек",
  },
  "Review the prepared post, then choose a channel and time.": {
    uz: "Tayyor postni tekshiring, so‘ng kanal va vaqtni tanlang.",
    ru: "Проверьте готовый пост, затем выберите канал и время.",
  },
  "Prepared image added to the post.": {
    uz: "Tayyor tasvir postga qo‘shildi.",
    ru: "Готовое изображение добавлено в пост.",
  },
  "Mark as reviewed": {
    uz: "Tekshirildi deb belgilash",
    ru: "Отметить проверенным",
  },
  "Checked:": {
    uz: "Tekshirildi:",
    ru: "Проверено:",
  },
  Verified: {
    uz: "Tekshirilgan",
    ru: "Проверено",
  },
  "Save reviewed text": {
    uz: "Tekshirilgan matnni saqlash",
    ru: "Сохранить проверенный текст",
  },
  "Reviewed post and destination saved to the delivery queue.": {
    uz: "Tekshirilgan post va manzil yuborish navbatiga saqlandi.",
    ru: "Проверенные пост и адрес сохранены в очереди отправки.",
  },
  "Review the approved post, image and channel destination, then confirm separately.":
    {
      uz: "Tekshirilgan post, tasvir va kanal manzilini ko‘rib, alohida tasdiqlang.",
      ru: "Проверьте одобренный пост, изображение и адрес канала, затем подтвердите отдельно.",
    },
  "Add a reviewed post to your plan. On disconnected channels it remains a plan.":
    {
      uz: "Tekshirilgan postni rejangizga qo‘shing. Ulanmagan kanalda u reja sifatida saqlanadi.",
      ru: "Добавьте проверенный пост в план. Для неподключённых каналов он остаётся планом.",
    },
  "Reviewed image analysis text": {
    uz: "Tekshirilgan tasvir tahlili matni",
    ru: "Проверенный текст анализа изображения",
  },
  Unverified: {
    uz: "Tekshirilmagan",
    ru: "Не проверено",
  },
  Review: {
    uz: "Tekshiring",
    ru: "Проверить",
  },
  "Needs review": {
    uz: "Tekshirish kerak",
    ru: "Нужна проверка",
  },
  "Needs review ·": {
    uz: "Tekshirish kerak ·",
    ru: "Нужна проверка ·",
  },
  "Review result": {
    uz: "Tekshirish natijasi",
    ru: "Результат проверки",
  },
  "Source text to review": {
    uz: "Tekshirish uchun manba matni",
    ru: "Исходный текст для проверки",
  },
  "Review and export use this exact image.": {
    uz: "Tekshirish va eksport aynan shu tasvirni ishlatadi.",
    ru: "Проверка и экспорт используют именно это изображение.",
  },
  "Check in progress": {
    uz: "Tekshiruv ishlayapti",
    ru: "Идёт проверка",
  },
  "Verification failed": {
    uz: "Tekshiruv o‘tmadi",
    ru: "Проверка не прошла",
  },
  "Textile object": {
    uz: "To‘qimachilik obyekti",
    ru: "Текстильный объект",
  },
  "This connection expired or belongs to another organization. Reconnect from Platform.":
    {
      uz: "Bu ulanish muddati tugagan yoki boshqa tashkilotga tegishli. Platform orqali qayta ulang.",
      ru: "Срок подключения истёк или оно принадлежит другой организации. Подключитесь заново через Platform.",
    },
  "This image has no readable dimensions.": {
    uz: "Bu tasvirning o‘lchamlarini o‘qib bo‘lmaydi.",
    ru: "Не удалось прочитать размеры изображения.",
  },
  "This image is unavailable. Choose a saved image from the media library.": {
    uz: "Bu tasvir mavjud emas. Kutubxonadan saqlangan tasvirni tanlang.",
    ru: "Изображение недоступно. Выберите сохранённое изображение из библиотеки.",
  },
  "This instance needs its Platform URL and organization binding.": {
    uz: "Ushbu nusxa uchun Platform manzili va tashkilot bog‘lanishi kerak.",
    ru: "Для этого экземпляра нужны адрес Platform и привязка организации.",
  },
  Restore: {
    uz: "Tiklash",
    ru: "Восстановить",
  },
  Language: {
    uz: "Til",
    ru: "Язык",
  },
  Languages: {
    uz: "Tillar",
    ru: "Языки",
  },
  "A typographic cover will be used.": {
    uz: "Tipografik muqova ishlatiladi.",
    ru: "Будет использована текстовая обложка.",
  },
  "Full outfit photo and a short heading.": {
    uz: "To‘liq kiyilgan foto va qisqa sarlavha.",
    ru: "Фото полного образа и короткий заголовок.",
  },
  Held: {
    uz: "To‘xtatilgan",
    ru: "Приостановлено",
  },
  "Not found": {
    uz: "Topilmadi",
    ru: "Не найдено",
  },
  Prompt: {
    uz: "Topshiriq",
    ru: "Задание",
  },
  "Tashkent time.": {
    uz: "Toshkent vaqti.",
    ru: "Время Ташкента.",
  },
  "View connection status": {
    uz: "Ulanish holatini ko‘rish",
    ru: "Состояние подключения",
  },
  Connections: {
    uz: "Ulanishlar",
    ru: "Подключения",
  },
  "Check connections": {
    uz: "Ulanishlarni tekshirish",
    ru: "Проверить подключения",
  },
  Disconnected: {
    uz: "Ulanmagan",
    ru: "Не подключено",
  },
  "For disconnected channels, the saved time is a manual publishing plan. Connecting the channel later does not send earlier plans automatically.":
    {
      uz: "Ulanmagan kanal uchun saqlangan vaqt qo‘lda joylash rejasidir. Kanal keyinroq sozlansa ham, oldingi rejalar o‘z-o‘zidan yuborilmaydi.",
      ru: "Для неподключённых каналов время сохраняется как план ручной публикации. Позднее подключение не отправляет старые планы автоматически.",
    },
  "For disconnected channels, download a ZIP package and publish manually. Actual delivery is configured separately and uses the approved post revision.":
    {
      uz: "Ulanmagan kanal uchun ZIP paketni yuklab, qo‘lda joylash mumkin. Haqiqiy yuborish alohida sozlanadi va tasdiqlangan post talqinini ishlatadi.",
      ru: "Для неподключённых каналов скачайте ZIP и опубликуйте вручную. Отправка настраивается отдельно и использует проверенную версию поста.",
    },
  Shares: {
    uz: "Ulashishlar",
    ru: "Репосты",
  },
  Unassigned: {
    uz: "Belgilanmagan",
    ru: "Не назначен",
  },
  "Unsaved changes discarded. Refresh to load the latest saved version.": {
    uz: "Saqlanmagan o‘zgarishlar bekor qilindi. Oxirgi saqlangan versiyani yuklash uchun yangilang.",
    ru: "Несохранённые изменения отменены. Обновите, чтобы загрузить последнюю сохранённую версию.",
  },
  "Unsaved changes stay in this view. Save them before searching or changing pages.":
    {
      uz: "Saqlanmagan o‘zgarishlar shu yerda qoladi. Qidirish yoki sahifani almashtirishdan oldin saqlang.",
      ru: "Несохранённые изменения остаются здесь. Сохраните их перед поиском или сменой страницы.",
    },
  "This request cannot be re-enabled. Check the connection and create a new plan.":
    {
      uz: "Ushbu niyat qayta yoqilmaydi. Ulanishni tekshirib, yangi reja yarating.",
      ru: "Этот запрос нельзя включить повторно. Проверьте подключение и создайте новый план.",
    },
  "Top line": {
    uz: "Ustki qator",
    ru: "Верхняя строка",
  },
  Time: {
    uz: "Vaqt",
    ru: "Время",
  },
  "Times use Tashkent. On disconnected channels the post remains a plan.": {
    uz: "Vaqt Toshkent bo‘yicha. Ulanmagan kanalda post reja sifatida saqlanadi.",
    ru: "Время Ташкента. На неподключённых каналах пост остаётся планом.",
  },
  "Overdue plans": {
    uz: "Vaqti o‘tgan rejalar",
    ru: "Просроченные планы",
  },
  "Times use Tashkent. Planned means the post has not been published yet.": {
    uz: "Vaqtlar Toshkent bo‘yicha. “Rejada” holati post hali joylanmaganini bildiradi.",
    ru: "Время Ташкента. «Запланировано» означает, что пост ещё не опубликован.",
  },
  Purpose: {
    uz: "Vazifa",
    ru: "Назначение",
  },
  "Set its purpose": {
    uz: "Vazifasini belgilang",
    ru: "Задайте назначение",
  },
  versions: {
    uz: "versiyalar",
    ru: "версии",
  },
  version: {
    uz: "versiya",
    ru: "версия",
  },
  "Vertical offset": {
    uz: "Vertikal siljish",
    ru: "Сдвиг по вертикали",
  },
  "View activity": {
    uz: "Faollikni ko‘rish",
    ru: "Показать действия",
  },
  "Wardrobe puzzle": {
    uz: "Garderob masalasi",
    ru: "Задача гардероба",
  },
  "What should this person do?": {
    uz: "Bu kishi nima qilishi kerak?",
    ru: "Что должен сделать этот человек?",
  },
  "Reading X data may use credits. Data is read only when the button is pressed.":
    {
      uz: "X so‘rovi kredit sarflashi mumkin. Faqat tugma bosilganda o‘qiladi.",
      ru: "Чтение данных X может расходовать кредиты. Данные читаются только по нажатию кнопки.",
    },
  "Dismiss message": {
    uz: "Xabarni yopish",
    ru: "Закрыть сообщение",
  },
  Failed: {
    uz: "Xato",
    ru: "Ошибка",
  },
  "No failed, unknown or overdue authorized deliveries.": {
    uz: "Xato, noma’lum yoki kechikkan tasdiqlangan yuborish yo‘q.",
    ru: "Нет ошибочных, неизвестных или просроченных разрешённых отправок.",
  },
  Service: {
    uz: "Xizmat",
    ru: "Сервис",
  },
  "Refresh service status": {
    uz: "Xizmat holatini yangilash",
    ru: "Обновить статус сервиса",
  },
  "Service model name · optional": {
    uz: "Xizmatdagi model nomi · ixtiyoriy",
    ru: "Имя модели в сервисе · необязательно",
  },
  "Configure the service and submit a prompt with a clear source.": {
    uz: "Xizmatni sozlang va aniq manba bilan topshiriq yuboring.",
    ru: "Настройте сервис и отправьте задание с указанным источником.",
  },
  "The same composition on a dark background.": {
    uz: "Xuddi shu kompozitsiya to‘q fonda.",
    ru: "Та же композиция на тёмном фоне.",
  },
  Sun: {
    uz: "Yak",
    ru: "Вс",
  },
  Conclusion: {
    uz: "Yakun",
    ru: "Заключение",
  },
  "Closing question": {
    uz: "Yakuniy savol",
    ru: "Заключительный вопрос",
  },
  "new ·": {
    uz: "yangi ·",
    ru: "новых ·",
  },
  "New story": {
    uz: "Yangi hikoya",
    ru: "Новая статья",
  },
  "Add a new measurement": {
    uz: "Yangi o‘lchov qo‘shish",
    ru: "Добавить новое измерение",
  },
  "New changes are unsaved. Save them first.": {
    uz: "Yangi o‘zgarishlar saqlanmagan. Avval ularni saqlang.",
    ru: "Новые изменения не сохранены. Сначала сохраните их.",
  },
  "in a new window": {
    uz: "yangi oynada",
    ru: "в новом окне",
  },
  "New post": {
    uz: "Yangi post",
    ru: "Новый пост",
  },
  "No new signals": {
    uz: "Yangi signal yo‘q",
    ru: "Новых сигналов нет",
  },
  "New image": {
    uz: "Yangi tasvir",
    ru: "Новое изображение",
  },
  "Review the generated image against the product’s shape and fabric. The prepared caption stays separate.":
    {
      uz: "Yangi tasvir mahsulot shakli va matosiga mosligi bo‘yicha tekshiriladi. Tayyor post matni alohida qoladi.",
      ru: "Проверьте соответствие созданного изображения форме и ткани товара. Подготовленный текст остаётся отдельно.",
    },
  "updated ·": {
    uz: "yangilandi ·",
    ru: "обновлено ·",
  },
  "Refreshing…": {
    uz: "Yangilanmoqda…",
    ru: "Обновление…",
  },
  "Upcoming plan": {
    uz: "Yaqin reja",
    ru: "Ближайший план",
  },
  "Upcoming plans": {
    uz: "Yaqin rejalar",
    ru: "Ближайшие планы",
  },
  "No generated images yet": {
    uz: "Yaratilgan tasvirlar hali yo‘q",
    ru: "Созданных изображений пока нет",
  },
  "Half fabric + product": {
    uz: "Yarim mato + mahsulot",
    ru: "Половина ткани + товар",
  },
  Close: {
    uz: "Yopish",
    ru: "Закрыть",
  },
  Enabled: {
    uz: "Yoqilgan",
    ru: "Включено",
  },
  Likes: {
    uz: "Yoqtirishlar",
    ru: "Лайки",
  },
  "Light background": {
    uz: "Yorug‘ fon",
    ru: "Светлый фон",
  },
  "Your changes stay here.": {
    uz: "O‘zgarishlaringiz shu yerda qoladi.",
    ru: "Ваши изменения остаются здесь.",
  },
  "The record enters immutable history. To correct an error, add a new observation later.":
    {
      uz: "Yozuv o‘zgarmas tarixga qo‘shiladi. Xatoni tuzatish uchun keyin yangi kuzatuv qo‘shing.",
      ru: "Запись добавляется в неизменяемую историю. Для исправления ошибки добавьте новое наблюдение.",
    },
  "latest records out of": {
    uz: "yozuvdan eng so‘nggi",
    ru: "последних записей из",
  },
  Sending: {
    uz: "Yuborilmoqda",
    ru: "Отправляется",
  },
  "Delivery queue": {
    uz: "Yuborish navbati",
    ru: "Очередь отправки",
  },
  "Delivery queue item cancelled.": {
    uz: "Yuborish navbati bekor qilindi.",
    ru: "Отправка в очереди отменена.",
  },
  "Add to delivery queue": {
    uz: "Yuborish navbatiga qo‘shish",
    ru: "Добавить в очередь отправки",
  },
  "Delivery permissions": {
    uz: "Yuborish ruxsatlari",
    ru: "Разрешения на отправку",
  },
  "Delivery images must be PNG or JPEG.": {
    uz: "Yuborish tasviri PNG yoki JPEG bo‘lishi kerak.",
    ru: "Для отправки изображение должно быть PNG или JPEG.",
  },
  "Authorize delivery": {
    uz: "Yuborishga ruxsat berish",
    ru: "Разрешить отправку",
  },
  "Review delivery": {
    uz: "Yuborishni ko‘rib chiqish",
    ru: "Проверить отправку",
  },
  "Loading…": {
    uz: "Yuklanmoqda…",
    ru: "Загрузка…",
  },
  Download: {
    uz: "Yuklash",
    ru: "Скачать",
  },
  "Move up": {
    uz: "Yuqoriga",
    ru: "Вверх",
  },
  "Interface language": {
    uz: "Interfeys tili",
    ru: "Язык интерфейса",
  },
  "Content language is independent of the interface.": {
    uz: "Kontent tili interfeys tilidan mustaqil.",
    ru: "Язык материала не зависит от языка интерфейса.",
  },
  "The original image could not be loaded. Close the tools and retry; your cover is unchanged.":
    {
      uz: "Asl tasvirni yuklab bo‘lmadi. Vositalarni yopib qayta urinib ko‘ring; muqova o‘zgarmadi.",
      ru: "Не удалось загрузить исходное изображение. Закройте инструменты и повторите; обложка не изменилась.",
    },
  "{count} matching enquiries": {
    uz: "{count} ta mos so‘rov",
    ru: "Найдено обращений: {count}",
  },
  "Unknown outcome. Check the channel history manually; this operation will not be sent again.":
    {
      uz: "Natija noma’lum. Kanal tarixini qo‘lda tekshiring; shu amal qayta yuborilmaydi.",
      ru: "Результат неизвестен. Проверьте историю канала вручную; эта операция не будет отправлена повторно.",
    },
  "Topic “{name}” saved.": {
    uz: "“{name}” bo‘limi saqlandi.",
    ru: "Раздел «{name}» сохранён.",
  },
  "Image editor": {
    uz: "Tasvir tahrirchisi",
    ru: "Редактор изображения",
  },
  "Edit history": {
    uz: "Tahrir tarixi",
    ru: "История изменений",
  },
  Undo: {
    uz: "Bekor qilish",
    ru: "Отменить",
  },
  Redo: {
    uz: "Qaytarish",
    ru: "Повторить",
  },
  "Reset edits": {
    uz: "Tahrirlarni tiklash",
    ru: "Сбросить изменения",
  },
  "Original image · crop before rotation": {
    uz: "Asl tasvir · aylantirishdan oldin qirqish",
    ru: "Исходное изображение · обрезка до поворота",
  },
  "Crop aspect ratio": {
    uz: "Qirqish tomonlar nisbati",
    ru: "Пропорции обрезки",
  },
  "Free crop": {
    uz: "Erkin qirqish",
    ru: "Свободная обрезка",
  },
  Square: {
    uz: "Kvadrat",
    ru: "Квадрат",
  },
  "Portrait 4:5": {
    uz: "Tik 4:5",
    ru: "Вертикально 4:5",
  },
  "Landscape 16:9": {
    uz: "Yotiq 16:9",
    ru: "Горизонтально 16:9",
  },
  "Full image": {
    uz: "To‘liq tasvir",
    ru: "Всё изображение",
  },
  "Processed preview": {
    uz: "Ishlangan ko‘rinish",
    ru: "Обработанный предпросмотр",
  },
  "Preview of the requested image edits": {
    uz: "So‘ralgan tahrirlar ko‘rinishi",
    ru: "Предпросмотр запрошенных изменений",
  },
  "Choose Preview edits to inspect the processed result. The original stays unchanged.":
    {
      uz: "Natijani ko‘rish uchun Tahrirlarni ko‘rishni tanlang. Asl tasvir o‘zgarmaydi.",
      ru: "Выберите предпросмотр изменений, чтобы проверить результат. Оригинал не изменится.",
    },
  "Rotate and flip": {
    uz: "Aylantirish va akslantirish",
    ru: "Поворот и отражение",
  },
  "Rotate left": {
    uz: "Chapga aylantirish",
    ru: "Повернуть влево",
  },
  "Rotate right": {
    uz: "O‘ngga aylantirish",
    ru: "Повернуть вправо",
  },
  "Flip horizontal": {
    uz: "Gorizontal akslantirish",
    ru: "Отразить по горизонтали",
  },
  "Flip vertical": {
    uz: "Vertikal akslantirish",
    ru: "Отразить по вертикали",
  },
  "Output width": {
    uz: "Natija eni",
    ru: "Ширина результата",
  },
  "Output height": {
    uz: "Natija balandligi",
    ru: "Высота результата",
  },
  "Keep proportions": {
    uz: "Nisbatni saqlash",
    ru: "Сохранять пропорции",
  },
  "Output format": {
    uz: "Natija formati",
    ru: "Формат результата",
  },
  "Output quality (JPEG and WebP)": {
    uz: "Natija sifati (JPEG va WebP)",
    ru: "Качество результата (JPEG и WebP)",
  },
  "Remove background": {
    uz: "Fonni olib tashlash",
    ru: "Удалить фон",
  },
  "Background removal is unavailable for this image.": {
    uz: "Bu tasvir uchun fonni olib tashlash mavjud emas.",
    ru: "Удаление фона для этого изображения недоступно.",
  },
  "Processing image…": {
    uz: "Tasvir ishlanmoqda…",
    ru: "Обработка изображения…",
  },
  "Preview edits": {
    uz: "Tahrirlarni ko‘rish",
    ru: "Предпросмотр изменений",
  },
  "Apply as new image": {
    uz: "Yangi tasvir sifatida qo‘llash",
    ru: "Применить как новое изображение",
  },
  "The image changed. Reset edits before continuing.": {
    uz: "Tasvir o‘zgardi. Davom etishdan oldin tahrirlarni tiklang.",
    ru: "Изображение изменилось. Перед продолжением сбросьте изменения.",
  },
  "Image dimensions exceed the processing limits or are unavailable.": {
    uz: "Tasvir o‘lchamlari chegaradan oshgan yoki mavjud emas.",
    ru: "Размеры изображения превышают ограничения или недоступны.",
  },
  "Keep the crop inside the original image.": {
    uz: "Qirqish asl tasvir ichida bo‘lsin.",
    ru: "Область обрезки должна быть внутри исходного изображения.",
  },
  "Choose a quarter-turn rotation.": {
    uz: "90 darajali aylantirishni tanlang.",
    ru: "Выберите поворот, кратный 90 градусам.",
  },
  "Choose positive output dimensions within the processing limits.": {
    uz: "Chegaralar ichida musbat natija o‘lchamlarini tanlang.",
    ru: "Выберите положительные размеры результата в пределах ограничений.",
  },
  "Choose PNG, JPEG or WebP and quality from 1 to 100.": {
    uz: "PNG, JPEG yoki WebP va 1 dan 100 gacha sifat tanlang.",
    ru: "Выберите PNG, JPEG или WebP и качество от 1 до 100.",
  },
  "Background removal is unavailable. Turn it off to continue.": {
    uz: "Fonni olib tashlash mavjud emas. Davom etish uchun o‘chiring.",
    ru: "Удаление фона недоступно. Отключите его для продолжения.",
  },
  "Markdown formatting": {
    uz: "Markdown formatlash",
    ru: "Форматирование Markdown",
  },
  "View mode": {
    uz: "Ko‘rinish rejimi",
    ru: "Режим просмотра",
  },
  Preview: {
    uz: "Ko‘rish",
    ru: "Предпросмотр",
  },
  "Heading 1": {
    uz: "1-sarlavha",
    ru: "Заголовок 1",
  },
  "Heading 2": {
    uz: "2-sarlavha",
    ru: "Заголовок 2",
  },
  Bold: {
    uz: "Qalin",
    ru: "Жирный",
  },
  Italic: {
    uz: "Qiya",
    ru: "Курсив",
  },
  "Inline code": {
    uz: "Satr ichidagi kod",
    ru: "Код в строке",
  },
  List: {
    uz: "Ro‘yxat",
    ru: "Список",
  },
  Blockquote: {
    uz: "Iqtibos",
    ru: "Цитата",
  },
  "Choose file": {
    uz: "Fayl tanlash",
    ru: "Выбрать файл",
  },
  "Loading options…": {
    uz: "Variantlar yuklanmoqda…",
    ru: "Загрузка вариантов…",
  },
  "Clear selection": {
    uz: "Tanlovni tozalash",
    ru: "Очистить выбор",
  },
  "Search…": {
    uz: "Qidirish…",
    ru: "Поиск…",
  },
  "No results": {
    uz: "Natija yo‘q",
    ru: "Нет результатов",
  },
  "Pick a date": {
    uz: "Sana tanlang",
    ru: "Выберите дату",
  },
  "Pick a time": {
    uz: "Vaqt tanlang",
    ru: "Выберите время",
  },
  Increase: {
    uz: "Oshirish",
    ru: "Увеличить",
  },
  Decrease: {
    uz: "Kamaytirish",
    ru: "Уменьшить",
  },
  Edit: {
    uz: "Tahrirlash",
    ru: "Редактор",
  },
  Split: {
    uz: "Yonma-yon",
    ru: "Рядом",
  },
  "View {title}": {
    uz: "{title} — ko‘rish",
    ru: "Открыть {title}",
  },
  "Remove {product}": {
    uz: "{product} ni olib tashlash",
    ru: "Удалить {product}",
  },
  "Related story {number}": {
    uz: "Bog‘liq hikoya {number}",
    ru: "Связанная статья {number}",
  },
  "Imported {created} drafts; preserved {skipped} existing items.": {
    uz: "{created} ta qoralama import qilindi; {skipped} ta mavjud yozuv saqlandi.",
    ru: "Импортировано черновиков: {created}; сохранено существующих записей: {skipped}.",
  },
  "Object ID": {
    uz: "Obyekt IDsi",
    ru: "ID объекта",
  },
  Collection: {
    uz: "To‘plam",
    ru: "Коллекция",
  },
  Place: {
    uz: "Joy",
    ru: "Место",
  },
  Period: {
    uz: "Davr",
    ru: "Период",
  },
  Materials: {
    uz: "Materiallar",
    ru: "Материалы",
  },
  Technique: {
    uz: "Texnika",
    ru: "Техника",
  },
  Dimensions: {
    uz: "O‘lchamlar",
    ru: "Размеры",
  },
  Maker: {
    uz: "Usta",
    ru: "Мастер",
  },
  "Attribution note": {
    uz: "Nisbat berish izohi",
    ru: "Примечание об атрибуции",
  },
  Kind: {
    uz: "Tur",
    ru: "Вид",
  },
  Credit: {
    uz: "Muallif",
    ru: "Авторство",
  },
  Rights: {
    uz: "Huquqlar",
    ru: "Права",
  },
  Region: {
    uz: "Hudud",
    ru: "Регион",
  },
  Fabric: {
    uz: "Mato",
    ru: "Ткань",
  },
  Motif: {
    uz: "Naqsh",
    ru: "Узор",
  },
  Colour: {
    uz: "Rang",
    ru: "Цвет",
  },
  Front: {
    uz: "Old",
    ru: "Спереди",
  },
  Detail: {
    uz: "Detal",
    ru: "Деталь",
  },
  Reference: {
    uz: "Manba",
    ru: "Источник",
  },
  Generated: {
    uz: "Yaratilgan",
    ru: "Создано",
  },
  Hero: {
    uz: "Asosiy",
    ru: "Главное",
  },
  New: {
    uz: "Yangi",
    ru: "Новое",
  },
  Contacted: {
    uz: "Aloqa qilingan",
    ru: "Связались",
  },
  Qualified: {
    uz: "Saralangan",
    ru: "Квалифицировано",
  },
  Closed: {
    uz: "Yopilgan",
    ru: "Закрыто",
  },
  Places: {
    uz: "Joylar",
    ru: "Места",
  },
  Techniques: {
    uz: "Texnikalar",
    ru: "Техники",
  },
  Motifs: {
    uz: "Naqshlar",
    ru: "Узоры",
  },
  Occasions: {
    uz: "Vaziyatlar",
    ru: "Поводы",
  },
  "Related textiles": {
    uz: "Bog‘liq matolar",
    ru: "Связанный текстиль",
  },
  "The operation failed ({status}). Try again.": {
    uz: "Amal bajarilmadi ({status}). Qayta urinib ko‘ring.",
    ru: "Операция не выполнена ({status}). Попробуйте снова.",
  },
  "The service returned error {status}.": {
    uz: "Xizmat {status} xato qaytardi.",
    ru: "Сервис вернул ошибку {status}.",
  },
  "Scheduled: {date}.": {
    uz: "Rejalashtirildi: {date}.",
    ru: "Запланировано: {date}.",
  },
  "Revision {revision} restored.": {
    uz: "{revision}-versiya tiklandi.",
    ru: "Версия {revision} восстановлена.",
  },
};
/** Legacy API and workflow messages are normalized without rewriting saved documents. */
const aliases: Readonly<Record<string, string>> = {
  "Amal mavjud holat bilan mos kelmadi. Ma’lumotlar saqlanib turibdi; holatni tekshiring.":
    "This operation no longer matches the current state. Your data is preserved; check the status.",
  "Qo‘lda joylash; tasdiqlangan avtomatik ulanish mavjud emas":
    "Manual publishing; no verified automatic connection",
  "Ocharo tahririy ilovasi; ijtimoiy tarmoq yuborishidan alohida":
    "Ocharo editorial app; separate from social delivery",
  "-talqin": " revision",
  "-talqin ·": " revision ·",
  "-talqin. Qo‘shilgach postni alohida saqlang va tekshiring.":
    " revision. Save and review the post separately after adding it.",
  "-ulanish": " connection",
  "-variant": " variant",
  ". Tahrirlar bir-biriga hreflang bilan bog‘lanadi; o‘zbekcha /uz/, ruscha /ru/ ostida.":
    ". Editions link to each other with hreflang; Uzbek uses /uz/ and Russian /ru/.",
  "· Izlanish": "· Research",
  "· Joylandi:": "· Published:",
  "· Kanal ko‘rsatkichlarini o‘qish:": "· Read channel metrics:",
  "· Manzil:": "· Destination:",
  "· oxirgi rejalashtirish:": "· Last scheduling:",
  "· Qamrov": "· Reach",
  "· Qo‘lda": "· Manual",
  "· Rejalashtirish urinishi:": "· Scheduling attempt:",
  "· Tasdiq": "· Confirmation",
  "· Toshkent": "· Tashkent",
  "· Yakun sanasi kirmaydi.": "· End date is excluded.",
  "“Haftada tasdiqlandi” kanal tasdig‘i kelgan vaqtni bildiradi; eski post keyin tasdiqlanishi mumkin. Qo‘lda joylanganlar o‘z joylash sanasi bo‘yicha sanaladi. Rejaga bog‘lanmagan qo‘lda yozuv bir xil postni qayd etishi mumkin; ikki ustun qo‘shilmaydi.":
    "Confirmed this week counts when channel confirmation arrived; older posts may be confirmed later. Manual posts use their publication date. An unlinked manual record may describe the same post; the two columns are not added together.",
  "“Keep reading” (bo‘sh — avtomatik)": "Keep reading (blank — automatic)",
  "): saytda logotip va havolalar o‘sha yerdan olinadi. Bu yerdagi qiymatlar faqat Brend markazi ishlamay qolganda ishlatiladi.":
    "): the site uses its logo and links. These values are used only when the Brand hub is unavailable.",
  "{name} {fabric} {decoration} {sizes} {price} {wholesale} {brand} — bo‘sh qiymatli qator tushib qoladi.":
    "{name} {fabric} {decoration} {sizes} {price} {wholesale} {brand} — lines with empty values are omitted.",
  "1 dona": "Single item",
  "1–3 manba tasvirni tanlang. Ko‘rinadigan belgilar va matn g‘oyasi taklif qilinadi. Mato tarkibi, o‘lcham va narx rasm bilan tasdiqlanmaydi.":
    "Choose 1–3 source images. Visible details and caption ideas are suggested. Images do not verify fabric composition, sizes or prices.",
  "1. Mahsulot": "1. Product",
  "2. Shablon": "2. Template",
  "3. Matn va tekshirish": "3. Text and review",
  "30 kun": "30 days",
  "3D kiyim studiyasi": "3D garment studio",
  "7 kun": "7 days",
  "90 kun": "90 days",
  "A yoki B": "A or B",
  "AI taklifi": "AI suggestion",
  "AI tasvir": "AI image",
  Akkaunt: "Account",
  "Akkaunt va manzil": "Account and destination",
  "Akkaunt:": "Account:",
  "Akkauntni tekshirish": "Verify account",
  "Amal bajarilmadi.": "The operation failed.",
  Arxiv: "Archive",
  Arxivda: "Archived",
  "Asl AI taklifi": "Original AI suggestion",
  "Asl foto": "Original photo",
  "Asl foto, mos mato yoki ish detallari uchun.":
    "For original photos, matching fabric or craftsmanship details.",
  Asosiy: "Main",
  "Asosiy bo‘limga o‘tish": "Skip to main content",
  "Asosiy bo‘limlar": "Main sections",
  "Asosiy manzil (RSS, sitemap uchun)": "Base URL (for RSS and sitemap)",
  "Atlas mahsuloti": "Atlas product",
  "Atlas yuklab olindi.": "Atlas downloaded.",
  "Atlasda taqqoslang; kundalik ishda tayyor variantni qo‘llang.":
    "Compare in Atlas; apply the chosen variant in everyday work.",
  "Atlasda taqqoslash →": "Compare in Atlas →",
  "Atlasni ko‘rish": "View Atlas",
  Auditoriya: "Audience",
  "Auditoriya, mavzular, egasi va haftalik maqsadni belgilang. Reja, haqiqiy yuborish va qo‘lda qayd etilgan postlar alohida hisoblanadi.":
    "Set the audience, topics, owner and weekly target. Plans, actual deliveries and manual posts are counted separately.",
  "Avtomatik joylash hozir o‘chirilgan. Qo‘lda joylash rejasi saqlanadi.":
    "Automatic publishing is currently off. Manual publishing plans are saved.",
  "Avtomatik rejim": "Automatic mode",
  "Avtomatik yuborish": "Automatic delivery",
  Avtomatika: "Automation",
  "Avtomatika sozlamalari saqlandi.": "Automation settings saved.",
  "Avval karta tasvirini tayyorlang": "Prepare the card image first",
  "Avval manba tasvirini kutubxonaga qo‘shing.":
    "Add the source image to the library first.",
  "Avval qoralama sifatida saqlang.": "Save as a draft first.",
  "Avvalgi holat quyida saqlangan.": "The previous state is preserved below.",
  "Aynan yuboriladigan matn va tasvirlar": "The exact text and images to send",
  Bajarilgan: "Completed",
  Bajarilmoqda: "Running",
  "Barcha bo‘limlar": "All topics",
  "Barcha holatlar": "All statuses",
  "Barcha rejalar": "All plans",
  "Barcha saqlangan": "All saved",
  "Barcha tasvirlar": "All images",
  "Barcha tillar": "All languages",
  Barchasi: "All",
  "Bekor qilingan": "Cancelled",
  "Bekor qilish": "Cancel",
  "Bir mahsulot, turli ifoda.": "One product, different expressions.",
  "Bir mahsulot. Bir yangi post.": "One product. One new post.",
  "Birinchi post": "First post",
  "Birinchi postni yaratish": "Create the first post",
  "Blog hikoyalari": "Blog stories",
  "Blog hikoyasi uchun post qoralamasi yaratildi.":
    "A post draft for the blog story was created.",
  "Blog studiyasi": "Blog studio",
  "Blog xizmatiga ulanib bo‘lmadi. Xizmat ishlayotganini tekshiring.":
    "Unable to connect to the blog service. Check that it is running.",
  "Bo‘lim": "Topic",
  "Bo‘lim o‘chirildi.": "Topic deleted.",
  "Bo‘lim o‘chirilsinmi? Ichida hikoya bo‘lsa, rad etiladi.":
    "Delete this topic? Topics containing stories cannot be deleted.",
  "Bo‘lim sarlavhasi": "Topic heading",
  "Bo‘limlar": "Topics",
  "Bo‘limni o‘chirish": "Delete topic",
  "Bo‘sh vaqtlarga joylash": "Fill available slots",
  "Bosh rasm": "Hero image",
  "Bosh sahifa sarlavhasi (title)": "Homepage title",
  "Bosh sahifa tavsifi": "Homepage description",
  "Bosh sahifadagi asosiy hikoya": "Featured homepage story",
  "Boshqa nomni sinab ko‘ring yoki filtrni o‘zgartiring.":
    "Try another name or change the filter.",
  Brend: "Brand",
  "Brend markazi ulangan (nashr": "Brand hub connected (revision",
  "Bu bo‘limni ochib bo‘lmadi.": "Unable to open this section.",
  "Bu davr uchun qo‘lda kuzatuv yo‘q. Natija nol deb hisoblanmaydi.":
    "No manual observations for this period. Missing data is not counted as zero.",
  "Bu hikoya:": "This story:",
  "Bu kanal tanlangan post formatini qo‘llamaydi.":
    "This channel does not support the selected post format.",
  "Bu kanalda avtomatik yuborish yo‘li hali mavjud emas.":
    "Automatic delivery is not yet available for this channel.",
  "Bu kanalga yuborish yoqilmagan.": "Delivery to this channel is not enabled.",
  "Bu maqsad avtomatik jadval yaratmaydi. Vaqtlarni Avtomatika bo‘limida belgilang.":
    "This target does not create a schedule. Set times in Automation.",
  "Bu rejada yuborish tarixi mavjud. Yangi niyat uchun yangi reja yarating.":
    "This plan already has delivery history. Create a new plan for a new attempt.",
  "Bu sahifa topilmadi.": "This page was not found.",
  "Bu so‘rov boshlangan. Holati noma’lum bo‘lsa pulli so‘rov avtomatik takrorlanmaydi.":
    "This request has started. A paid request with an unknown outcome is not retried automatically.",
  "Bu tasdiq belgilangan vaqtda aynan shu postni shu kanalga yuborishga ruxsat beradi.":
    "This confirmation authorizes this exact post to this channel at the scheduled time.",
  "Bu tugma kanal ma’lumotini o‘qiydi. Post yubormaydi.":
    "This button reads channel data. It does not send a post.",
  "Bu X o‘qish so‘rovi kredit sarflashi mumkin. Yangi post yuborilmaydi.":
    "This X read request may use credits. No new post will be sent.",
  "Bu yuborish yo‘li faqat bitta tasvirni qo‘llaydi.":
    "This delivery method supports only one image.",
  Bugun: "Today",
  "Bular yuborishga ruxsat berilmagan rejalar. Qo‘lda joylangan bo‘lsa, Natijalar bo‘limida qayd eting.":
    "These plans are not authorized for delivery. Record manually published posts in Results.",
  Buyurtmalar: "Orders",
  Ch: "Wed",
  Chor: "Wed",
  "daq ·": "min ·",
  "daq o‘qish": "min read",
  "Davom ettiring": "Continue",
  Detallar: "Details",
  Du: "Mon",
  Dush: "Mon",
  "E’tibor talab qiladigan yuborishlar": "Deliveries needing attention",
  Eksport: "Export",
  "Eng so‘nggi 250 kuzatuv ko‘rsatilgan; oldingi tarix saqlanib turibdi.":
    "Showing the latest 250 observations; earlier history is preserved.",
  "Eng so‘nggisi": "Latest",
  "Faktlardan matn": "Text from facts",
  Faol: "Active",
  Faollik: "Engagement",
  "Faollik noma’lum": "Engagement unknown",
  "Faqat hali yuborilmagan reja navbatga qo‘shiladi.":
    "Only plans that have not been sent can enter the queue.",
  "Faqat matn yuboriladi.": "Only text will be sent.",
  "Fayl tanlash": "Choose a file",
  "Fayl va manba izohini kiriting.": "Select a file and enter a source note.",
  Formatlar: "Formats",
  "Formatlar · mas’ul": "Formats · owner",
  "Foto tayyorlash": "Prepare photo",
  "Gorizontal siljish": "Horizontal offset",
  "Hafta:": "Week:",
  "Haftada tasdiqlandi": "Confirmed this week",
  "Haftalik maqsad": "Weekly target",
  "Haftalik vaqtlar": "Weekly slots",
  "Haftasiga postlar": "Posts per week",
  "Hali kanalga yuborish tarixi yo‘q. Rejadagi postlar bu ro‘yxatga kirmaydi.":
    "No channel delivery history yet. Planned posts are not included in this list.",
  "Hali kuzatilmagan": "Not yet observed",
  "Hali reja yo‘q": "No plans yet",
  "Hali saqlangan tahlil yo‘q. Tanlangan tasvirdan boshlang.":
    "No saved analysis yet. Start with the selected image.",
  "Hammasi saqlangan": "All changes saved",
  "Haqiqiy joylash tasdig‘i, tekshirilgan ulanish va ko‘rsatkichlarni o‘qish ruxsati kerak. Quyida faqat avval saqlangan kuzatuvlar ko‘rsatiladi.":
    "Requires a confirmed publication, a verified connection and permission to read metrics. Only saved observations appear below.",
  "Haqiqiy mato va ish yaqin fotosi.":
    "Close-up of the actual fabric and craftsmanship.",
  "Haqiqiy natijalar uchun kanal kerak": "Connect a channel for real results",
  "Har bir kanalning vazifasi": "Each channel’s purpose",
  "Har bir matn, tanlangan tasvir va tekshirilgan variant bir joyda.":
    "Every caption, selected image and reviewed variant in one place.",
  "Har kanalning o‘z yo‘li bor.": "Every channel has its own approach.",
  "Har postning oxirgi kuzatuvi ko‘rsatiladi; takroriy jamlangan sonlar qo‘shilmaydi. Faollik = (yoqtirish + izoh + ulashish + saqlash) ÷ qamrov × 100. Biror son noma’lum bo‘lsa foiz hisoblanmaydi.":
    "Shows each post’s latest observation without adding repeated cumulative counts. Engagement = (likes + comments + shares + saves) ÷ reach × 100. No percentage is calculated when any value is unknown.",
  Havola: "Link",
  "Havola bosishlar": "Link clicks",
  "Havola manzili": "Link URL",
  "Havola nomi": "Link label",
  Hikoya: "Story",
  "Hikoya arxivlandi.": "Story archived.",
  "Hikoya ko‘rinishi": "Story preview",
  "Hikoya nashr qilindi — saytda ko‘rinadi.":
    "Story published — visible on the site.",
  "Hikoya saqlandi.": "Story saved.",
  "Hikoya saytdan olindi (qoralama).": "Story removed from the site (draft).",
  "Hikoya topilmadi.": "Story not found.",
  Hikoyalar: "Stories",
  "Hikoyalar studiyasi": "Story studio",
  "Hikoyalarga qaytish": "Back to stories",
  Holat: "Status",
  "Holatni tekshirish": "Check status",
  "Holatni yangilash": "Refresh status",
  "Hozir ko‘rishlar yoki faollik haqida ma’lumot yo‘q. Postlarni tayyorlash va qo‘lda joylash davom etishi mumkin.":
    "Views and engagement data are not available yet. You can continue preparing and manually publishing posts.",
  "Hozir mahalliy ish stoli": "Currently a local workspace",
  "Hozir nashr qilish": "Publish now",
  "Hozircha saqlash": "Save for now",
  "Ikki haqiqiy rang yoki dizayn varianti.":
    "Two actual colour or design variants.",
  Inglizcha: "English",
  "Instagram, Telegram yoki saytdagi post natijalarini sanasi va manbasi bilan kiriting. Bular operator yozuvlari; kanal API tasdig‘i emas.":
    "Enter Instagram, Telegram or website post results with the date and source. These are operator records, not channel API confirmations.",
  Iqtiboslar: "Quotes",
  "Ish stoli": "Desk",
  "Ish stoli yuklanmagan.": "Workspace has not loaded.",
  "Ish stoli yuklanmoqda…": "Loading workspace…",
  "Ish stoliga qaytib, kerakli bo‘limni tanlang.":
    "Return to the desk and choose a section.",
  "Ish stoliga qaytish": "Back to desk",
  "Ish stoliga ulanib bo‘lmadi.": "Unable to connect to the workspace.",
  "Ishga tushdi": "Started",
  "Ishga tushirish": "Run",
  "Ishlayotgan navbat xizmati kanalga ulanganlikni anglatmaydi. Har bir yuborish tasdiqlangan post, manzil va alohida ruxsat talab qiladi.":
    "A running queue service does not mean a channel is connected. Each delivery needs a reviewed post, destination and separate authorization.",
  Ixtiyoriy: "Optional",
  Izoh: "Note",
  "Izoh (figcaption)": "Caption (figcaption)",
  "Izoh 2000 belgidan oshmasin.": "The note must not exceed 2000 characters.",
  "Izoh va javoblar orqali rang yoki dizayn qiziqishini baholang.":
    "Assess interest in colours or designs through comments and replies.",
  Izohlar: "Comments",
  "Jadvalni saqlash": "Save schedule",
  Jamlangan: "Cumulative",
  "Jarayon videosi": "Process video",
  Javoblar: "Replies",
  "Jonli ko‘rinish": "Live preview",
  "Jonli yuborish:": "Live delivery:",
  Joylangan: "Published",
  "Joylangan post": "Published post",
  "Joylangan post havolasi": "Published post URL",
  "Joylangan vaqt · Toshkent": "Published at · Tashkent",
  "Joylash jadvali": "Publishing schedule",
  "Joylash tarixi": "Publishing history",
  "Joylash tasdig‘i:": "Publication confirmation:",
  "Joylashuvni tiklash": "Reset placement",
  Ju: "Fri",
  Jum: "Fri",
  Kalit: "Key",
  "kamida bitta bo‘lim va paragraf": "at least one section and paragraph",
  Kanal: "Channel",
  "Kanal akkaunti va manzil hali tekshirilmagan.":
    "The channel account and destination have not been verified yet.",
  "Kanal faqat shu tugma bosilganda o‘qiladi.":
    "The channel is read only when this button is pressed.",
  "Kanal kartasidan tahrirlashni oching. Dastlabki holat kanalga ulanmaydi va hech qanday post yubormaydi.":
    "Open editing from a channel card. The initial state does not connect a channel or send posts.",
  "Kanal qabul qilgan": "Accepted by channel",
  "Kanal rejasi saqlandi. Bu yuborishni yoqmaydi.":
    "Channel plan saved. This does not enable delivery.",
  "Kanal tarixini tekshiring. Qayta yuborish takroriy post yaratishi mumkin; bu ekran avtomatik qaytarmaydi.":
    "Check the channel history. Sending again may duplicate the post; this screen does not retry automatically.",
  "Kanal tasdig‘i:": "Channel confirmation:",
  "Kanal tasdig‘ini tekshirish": "Check channel confirmation",
  "Kanal yoki sahifa havolasi": "Channel or page URL",
  "Kanaldan yangilash": "Refresh from channel",
  Kanallar: "Channels",
  "Kanallar xaritasi": "Channel map",
  "Kanalni rejada faol deb belgilash": "Mark channel active in the plan",
  "Kanalning akkaunt va manzil holati tekshirildi.":
    "The channel account and destination status were checked.",
  "Karta tayyorlash": "Prepare card",
  Kartochka: "Card",
  "Kartochka iborasi": "Card phrase",
  Katalog: "Catalogue",
  "Katalog ulanmagan": "Catalogue disconnected",
  "Katalog urinishi:": "Catalogue attempt:",
  "Katalog va rejalashtirish": "Catalogue and scheduling",
  "Katalogda yangi mahsulot paydo bo‘lsa, avtomatik post qoralamasi yaratilsin":
    "Automatically create a post draft when a new product appears in the catalogue",
  "Katalogdagi mahsulotlar (hikoya ostida)":
    "Catalogue products (below the story)",
  "Katalogdan mahsulotlarni oling, bir bosishda post qoralamalarini yarating va tasdiqlanganlarini haftalik jadvalga joylang. Jadval faqat reja tuzadi: tarmoqqa yuborish uchun tasdiqlangan hisob va alohida navbat tasdig‘i kerak.":
    "Import catalogue products, create post drafts in one click and place approved posts into weekly slots. Scheduling creates a plan only; delivery needs a verified account and separate queue authorization.",
  "Katalogdan yangilash": "Refresh from catalogue",
  "Katalogni har necha soatda yangilash (0 — o‘chiq)":
    "Catalogue refresh interval in hours (0 — off)",
  "Kechikkan reja yo‘q.": "No overdue plans.",
  "Keng lenta": "Wide feed",
  "Keyingi 7 kun:": "Next 7 days:",
  "Keyingi hafta": "Next week",
  "Keyingi tahlil uchun": "For the next analysis",
  Kirish: "Introduction",
  "Kiyilgan ko‘rinish": "Worn look",
  "Ko‘rib chiqishga tayyor": "Ready for review",
  "Ko‘rinish o‘zgargan bo‘lsa, tasvirni qayta tayyorlang.":
    "Regenerate the image if the appearance changed.",
  "Ko‘rinish tayyorlanmoqda…": "Preparing preview…",
  "Ko‘rinishlar atlasi": "Visual atlas",
  "Ko‘rish": "View",
  "Ko‘rish davomiyligi va ulashishlarni kuzating.":
    "Track viewing duration and shares.",
  "Ko‘rishlar": "Views",
  "Ko‘rsatkichlar haqiqiy kuzatuv mavjud bo‘lganda ko‘rinadi. Yetishmayotgan ma’lumot nolga teng emas.":
    "Metrics appear when actual observations exist. Missing data is not zero.",
  "Ko‘rsatkichlarni o‘qish": "Read metrics",
  "Kontent kutubxonasi": "Content library",
  Kun: "Day",
  Kunlar: "Days",
  "Kutubxonadan tanlash": "Choose from library",
  "Kutubxonadan tanlash…": "Choose from library…",
  "Kutubxonadan tasvir tanlang yoki yangi kartani tayyorlang.":
    "Choose a library image or prepare a new card.",
  "Kutubxonadan tasvir tanlash ·": "Choose library image ·",
  "Kutubxonaga qo‘shish": "Add to library",
  "Kuzatildi:": "Observed:",
  Kuzatilgan: "Observed",
  "Kuzatilgan vaqt · Toshkent": "Observed at · Tashkent",
  Kuzatilmagan: "Not observed",
  "Kuzatuv davri": "Observation period",
  "Kuzatuv qo‘shish": "Add observation",
  "Kuzatuv sanasi yo‘q": "No observation date",
  "Kuzatuv tarixi": "Observation history",
  "Kuzatuv yo‘q": "No observation",
  "Kuzatuvni saqlash": "Save observation",
  Logotip: "Logo",
  "Logotip, ranglar, havolalar": "Logo, colours, links",
  "Ma'lumot yo'q": "Unavailable",
  "Ma’lumot yo‘q": "No data",
  "Ma’lumot yo‘q bo‘lsa bo‘sh qoldiring. Haqiqiy nol bo‘lsa 0 kiriting. So‘rov va buyurtmalar faqat siz tekshirgan bog‘lanishdir.":
    "Leave unknown values blank. Enter 0 only for a real zero. Enquiries and orders are linked only as verified by you.",
  "Ma’lumotlar o‘chirilmagan. Qayta urinib ko‘ring.":
    "Your data is preserved. Try again.",
  "Mahalliy holat har 30 soniyada yangilanadi. Tasdiqlanmagan reja yuborilmaydi; noma’lum yuborish avtomatik takrorlanmaydi.":
    "Local status refreshes every 30 seconds. Unapproved plans are not sent; deliveries with unknown outcomes are not retried automatically.",
  "Mahalliy ish stoli · Toshkent": "Local workspace · Tashkent",
  "Mahalliy xizmatni ishga tushirib, yana urinib ko‘ring.":
    "Start the local service and try again.",
  Mahsulot: "Product",
  "Mahsulot — diqqat markazida.": "The product takes centre stage.",
  "Mahsulot detali": "Product detail",
  "Mahsulot faktlari": "Product facts",
  "Mahsulot fotosi, yuklangan tasvir yoki tayyor kartani tanlang. Tartib tanlangan ketma-ketlikda saqlanadi.":
    "Choose a product photo, uploaded image or prepared card. Your selection order is preserved.",
  "Mahsulot haqida qisqa va aniq yozing…":
    "Write a concise, accurate product caption…",
  "Mahsulot haqidagi matnni joylang. Topilgan ma’lumot va yetishmayotgan qismlarni ko‘ring.":
    "Paste product source text. Review extracted facts and missing details.",
  "Mahsulot kartasi": "Product card",
  "Mahsulot ma’lumotlari": "Product information",
  "Mahsulot old ko‘rinishi": "Product front view",
  "Mahsulot rasmlari, tayyor shablonlar va bir joyda saqlangan rejangiz.":
    "Product photos, ready templates and your plans in one place.",
  "Mahsulot tanlang": "Choose a product",
  "Mahsulot tanlang…": "Choose a product…",
  "Mahsulot tanlash": "Select product",
  "Mahsulot va sarlavhani kiriting.": "Enter a product and title.",
  "Mahsulot, narx, rasm": "Product, price, image",
  "Mahsulotdan postgacha": "From product to post",
  "Mahsulotga mosligini tekshiring. Bu matn katalogni o‘zgartirmaydi.":
    "Check that it matches the product. This text does not change the catalogue.",
  Mahsulotlar: "Products",
  "Mahsulotlar va tasvirlar": "Products and images",
  "Mahsulotlarni tanlang — har biri uchun shablon asosida qoralama yaratiladi (tasdiqlash sizda).":
    "Select products to create template-based drafts for each one. You approve them.",
  "Mahsulotni topish": "Find a product",
  "Mahsulotni topish…": "Find a product…",
  "Mahsulotning old ko‘rinishi iliq och fonda.":
    "Product front view on a warm light background.",
  Manba: "Source",
  "Manba / dalil izohi": "Source / evidence note",
  "Manba izi": "Source provenance",
  "Manba izi o‘zgargan yoki tasvir mavjud emas. Bu taklifni hozir qabul qilib bo‘lmaydi.":
    "The source has changed or the image is unavailable. This suggestion cannot be accepted now.",
  "Manba izohi": "Source note",
  "Manba manzili": "Source URL",
  "Manba matnini tekshiring": "Review source text",
  "Manba nomi": "Source name",
  "Manba tahlili ·": "Source analysis ·",
  "Manba tasviri": "Source image",
  Manbalar: "Sources",
  "Manbalar ·": "Sources ·",
  "Manbalar:": "Sources:",
  "Manbani o‘chirish": "Remove source",
  "Manzil (/journal/…/)": "URL (/journal/…/)",
  "Marketing ish stoli": "Marketing desk",
  "Mas’ul": "Owner",
  "Masalan: Instagram Insights, shu postning qamrovi; 24 soatdan so‘ng o‘qildi.":
    "For example: Instagram Insights, reach for this post, read after 24 hours.",
  "Masalan: mahsulotni o‘zgartirmasdan, sokin studiya fonida ko‘rsating…":
    "For example: show the unchanged product against a quiet studio background…",
  "Masalan: rang va bichimni qisqa post matniga aylantirish.":
    "For example: turn colour and cut into a short post caption.",
  Masshtab: "Scale",
  "Masshtab: 75–105%. Siljish karta enining ±12% yoki balandligining ±6% chegarasida; 0.01 = 1%.":
    "Scale: 75–105%. Offset: ±12% of card width or ±6% of height; 0.01 = 1%.",
  "Material: … O‘lcham: … Narx: …": "Material: … Size: … Price: …",
  "Matn kiritilmagan": "No text entered",
  "Matn kiritilmagan.": "No text entered.",
  "Matn o‘zgargan. Postda ishlatishdan oldin tekshirilgan matnni saqlang.":
    "The text changed. Save the reviewed text before using it in a post.",
  "Matn shabloni": "Caption template",
  "Matn taklifi": "Text suggestion",
  "Matn tekshirilgan": "Text reviewed",
  "Matn va tanlangan variantni saqlab qoling yoki sahifadan chiqing.":
    "Save the text and chosen variant, or leave the page.",
  "Matn va tasvir xizmatlari": "Text and image services",
  Matnlar: "Texts",
  "Matnni tekshirish": "Review text",
  "Mato + mahsulot": "Fabric + product",
  "Mato fotosi": "Fabric photo",
  "Mato fotosi kerak": "Fabric photo required",
  "Mato tanlang": "Choose fabric",
  "Mavjud mahsulot rasmini tanlang va birinchi postingizni saqlang.":
    "Choose an existing product image and save your first post.",
  "Mavjud mahsulotdan boshlang. Rasmlar va manba matnini shu yerda tayyorlang.":
    "Start with an existing product. Prepare images and source text here.",
  "Mavjud mahsulotdan yangi ko‘rinish yoki fon taklifi.":
    "A new look or background suggestion for an existing product.",
  "Mavjud reja (ixtiyoriy)": "Existing plan (optional)",
  Mavzular: "Themes",
  "Mavzular / kontent yo‘nalishlari": "Themes / content directions",
  Menyuda: "In navigation",
  "Menyuda (": "In navigation (",
  "Meta tavsif": "Meta description",
  "Mobil bo‘limlar": "Mobile sections",
  "Model mavjud emas": "Model unavailable",
  "Model sozlamasi": "Model setting",
  "Mos mahsulot topilmadi.": "No matching product found.",
  "Mos mato": "Matching fabric",
  "Mos mato fotosi": "Matching fabric photo",
  "Mos mato fotosi o‘ng yarmida, mahsulot uning ustida.":
    "Matching fabric on the right half, with the product over it.",
  "Mos mato fotosini Mahsulotlar bo‘limiga yuklang.":
    "Upload a matching fabric photo in Products.",
  "Mos mato fotosini yuklab, mahsulotni ustiga joylashtiring.":
    "Upload a matching fabric photo and place the product over it.",
  "Mos post topilmadi": "No matching post found",
  Muqova: "Cover",
  "Muqova izohi": "Cover caption",
  "Muqova tavsifi": "Cover description",
  "muqova tavsifi va izohi": "cover description and caption",
  "Muqova, mahsulotlar va bog‘liq hikoyalar":
    "Cover, products and related stories",
  "Muqovani olib tashlash": "Remove cover",
  "Muvaffaqiyatli tekshiruv:": "Successful check:",
  "Narx kiritilmagan": "Price not set",
  Nashr: "Publication",
  "Nashr amallari oldidan o‘zgarishlarni saqlang.":
    "Save changes before publishing actions.",
  "Nashr izohi": "Publication note",
  "Nashr qilingan": "Published",
  "Nashrdan oldin qo‘shing:": "Add before publishing:",
  "Natija noma'lum": "Outcome unknown",
  "Natija shu yerda ko‘rinadi.": "The result will appear here.",
  Natijalar: "Results",
  "Natijalar tarixi": "Results history",
  "Natijalarni tekshirish (kun)": "Results check interval (days)",
  Navbatda: "Queued",
  "Navbatni bekor qilish": "Cancel queue item",
  "Necha kun oldinga rejalashtirish (0 — o‘chiq)":
    "Days to schedule ahead (0 — off)",
  "Nima odamlarni qiziqtirdi?": "What interested people?",
  "Nimani e’tiborga olish kerak?": "What needs attention?",
  "Noma’lum": "Unknown",
  Nomi: "Name",
  "Nomi (": "Name (",
  "o‘chiq": "off",
  "O‘chirilgan": "Disabled",
  "O‘chirish": "Delete",
  "O‘lcham, mato va xarid ma’lumotlari.": "Size, fabric and purchase details.",
  "O‘qish": "Read",
  "O‘quvchiga va’da (ichki, chop etilmaydi)":
    "Reader promise (internal, not published)",
  "O‘zgarish izohi": "Change note",
  "O‘zgarish izohi (ixtiyoriy)": "Change note (optional)",
  "O‘zgarmagan so‘rov qayta bosilsa yangi pulli tahlil yaratilmaydi. Manba yoki izohni o‘zgartirish yangi niyatni boshlaydi.":
    "Repeating an unchanged request does not create a new paid analysis. Changing the source or prompt starts a new request.",
  Obunachilar: "Followers",
  "Ocharo Blogdagi nashr qilingan hikoyani e’lon qiluvchi post qoralamasi.":
    "A post draft announcing a published Ocharo Blog story.",
  "Ocharo Katalog": "Ocharo Catalogue",
  "Ocharo Katalogdan yangilash": "Refresh from Ocharo Catalogue",
  "Ocharo platformasi": "Ocharo platform",
  "Old ko‘rinish": "Front view",
  "Old ko‘rinishdan boshlang.": "Start with the front view.",
  "Old ko‘rinishni tanlang, shablonni qo‘llang va postingizni tayyorlang.":
    "Choose the front view, apply a template and prepare your post.",
  "Old ko‘rinishni tanlash": "Choose front view",
  "Oldingi hafta": "Previous week",
  "Oldingi matnni tiklash": "Restore previous text",
  "olindi ·": "received ·",
  "OpenAI · Bu tugma pulli tasvir tahlilini boshlashi mumkin. Natija avtomatik qabul qilinmaydi.":
    "OpenAI · This button may start paid image analysis. Results are not accepted automatically.",
  "OpenAI hozir sozlanmagan. Tahlil mavjud emas. Holatni yangilash faqat saqlangan sozlamalarni o‘qiydi.":
    "OpenAI is not configured. Analysis is unavailable. Refreshing status reads saved settings only.",
  "Oxirgi katalog yangilanishi:": "Last catalogue refresh:",
  "Oxirgi so‘rov:": "Last request:",
  "Oxirgi tekshiruv xato": "Last check failed",
  "Oxirgi xato (": "Last error (",
  Pa: "Thu",
  "Paragraflar (bo‘sh qator bilan ajrating)":
    "Paragraphs (separate with blank lines)",
  Pastga: "Move down",
  "Pastki havolalar": "Footer links",
  "Pastki qator": "Bottom line",
  Pay: "Thu",
  "PDF postlarning aynan tanlangan PNG tasvirlarini ishlatadi. Izohlar kartadan tashqarida qoladi.":
    "The PDF uses the exact selected PNG images. Captions stay outside the cards.",
  "Post ko‘rinishi": "Post preview",
  "Post matni": "Post caption",
  "Post matniga qo‘shish": "Add to post caption",
  "Post nomi": "Post name",
  "Post qoralamasi": "Post draft",
  "Post rejaga qo‘shildi.": "Post added to the plan.",
  "Post saqlandi.": "Post saved.",
  "Post sarlavhasi": "Post title",
  "Post tasvirining asl manbasi topilmadi.":
    "The original source for this post image was not found.",
  "Post tili": "Post language",
  "Post to‘plami yuklab olindi.": "Post package downloaded.",
  "Post topilmadi": "Post not found",
  "Post va avtomatika": "Posts and automation",
  "Post yaratish": "Create post",
  "Post yaratishda tayyorlangan kartalar shu yerda paydo bo‘ladi.":
    "Cards prepared in the post composer will appear here.",
  "Post, manzil yoki reja o‘zgardi. Yangilangan holatni qayta ko‘rib chiqing.":
    "The post, destination or plan changed. Review the updated state.",
  "Postda ishlatish": "Use in post",
  "Postda qo‘llash": "Apply to post",
  "Postga qo‘shilgan tasvir": "Image added to post",
  "Postga qo‘shish": "Add to post",
  "Postingizga shakl bering.": "Give your post its shape.",
  Postlar: "Posts",
  "Postlar oqimi": "Post flow",
  "Postlar qayerda to‘xtadi?": "Where did posts stop?",
  "Postlar ro‘yxatidan mavjud variantni oching.":
    "Open an existing variant from the post list.",
  "Postlar taqvimi": "Post calendar",
  "Postlarga qaytish": "Back to posts",
  "Postni ochish": "Open post",
  "Postni rejalashtirish": "Schedule post",
  "Postni tahrirlash": "Edit post",
  "Postni tanlash": "Choose a post",
  "Postni tayyorlash": "Prepare post",
  "Postni topish": "Find a post",
  "Postni topish…": "Find a post…",
  "Postning jamlangan ko‘rsatkichlari": "Cumulative post metrics",
  Qamrov: "Reach",
  "Qavsdagi farq: shu havolaning oldingi qo‘lda kuzatuviga nisbatan. Kanal API sonlari qo‘shilmaydi.":
    "The difference in brackets compares the previous manual observation of the same URL. Channel API counts are not added.",
  "Qaysi fikrni tasdiqlaydi (ixtiyoriy)": "Supporting which claim (optional)",
  "Qayta ko‘rib chiqish": "Review again",
  "Qayta tekshirish kerak": "Needs rechecking",
  "Qayta urinish": "Retry",
  Qidirish: "Search",
  "Qidiruv tizimlaridan yashirish (noindex)":
    "Hide from search engines (noindex)",
  "Qisman olingan": "Partial",
  "qisqa tavsif": "short description",
  "Qisqa tavsif (kartochka va feed)": "Short description (card and feed)",
  "Qo‘lda kuzatilgan natijalar": "Manually observed results",
  "Qo‘lda kuzatuv saqlandi. Kanal tasdig‘i o‘zgarmadi.":
    "Manual observation saved. Channel confirmation is unchanged.",
  "Qo‘lda qayd etilgan": "Manually recorded",
  "Qo'llanmaydi": "Unsupported",
  "Qo‘llash": "Apply",
  "Qo‘shimcha satr": "Additional line",
  "Qo‘shish": "Add",
  "Qora fon": "Dark background",
  Qoralama: "Draft",
  "Qoralama bor": "Draft exists",
  "Qoralama yangilangan. Hozirgi talqinni ochish.":
    "The draft was updated. Open the current revision.",
  "qoralama yaratildi": "drafts created",
  Qoralamaga: "Move to draft",
  Qoralamalar: "Drafts",
  "Rasm qayerdan olingan?": "Where did the image come from?",
  "rasm saqlandi": "images saved",
  "Rasm tavsifi (alt)": "Image description (alt)",
  "Rasm uchun mahsulot": "Product for image",
  "Rasm yuklash": "Upload image",
  "Rasm:": "Image:",
  "Rasm: birinchi mahsulot": "Image: first product",
  "Rasmiy hujjat": "Official documentation",
  "Rasmiy ochilishgacha yoqilgan qoladi. O‘chirsangiz, robots.txt va sahifalar indekslashga ruxsat beradi.":
    "Keep enabled until the official launch. Turning it off allows indexing in robots.txt and pages.",
  "Rasmiy sozlash yo‘riqnomasi": "Official setup guide",
  Rasmlar: "Images",
  "Reja bekor qilindi.": "Plan cancelled.",
  "Reja belgilanmagan": "No plan set",
  "Reja vaqti kelajakda bo‘lishi kerak.":
    "The planned time must be in the future.",
  "reja. Natijalar:": "plans. Results:",
  Rejada: "Planned",
  "Rejada faol": "Active in plan",
  "Rejada:": "Planned:",
  "Rejadagi talqin hozirgi tekshirilgan post bilan mos emas. Yangi reja yarating.":
    "The planned revision no longer matches the reviewed post. Create a new plan.",
  Rejaga: "To plan",
  "Rejaga bog‘lanmagan": "Not linked to a plan",
  "Rejaga qo‘shish": "Add to plan",
  Rejalashtirilgan: "Scheduled",
  Rejalashtirish: "Scheduling",
  "Rejalashtirish (mahalliy vaqt)": "Schedule (local time)",
  "Rejalashtirish kanali": "Scheduling channel",
  "Rejangiz hali bo‘sh": "Your plan is still empty",
  "Rejani ko‘rish": "View plan",
  "Rejani ochish": "Open plan",
  "Rejani saqlash": "Save plan",
  "Rejani tahrirlash": "Edit plan",
  rejasi: "plan",
  "Ro‘yxat (har qatorda bittadan, ixtiyoriy)":
    "List (one item per line, optional)",
  Ruscha: "Russian",
  "Ruxsat bekor qilingan": "Authorization revoked",
  "Sahifa topilmadi": "Page not found",
  "Sahifa topilmadi.": "Page not found.",
  "Sana tekshirilishi kerak": "Date needs checking",
  "Sana va vaqt": "Date and time",
  "Sana va vaqtni tanlang": "Choose a date and time",
  "Sana va vaqtni tanlang.": "Choose a date and time.",
  "Sana va vaqtni tekshiring.": "Check the date and time.",
  "Sana va vaqtni to‘ldiring.": "Fill in the date and time.",
  "Saqlamasdan chiqish": "Leave without saving",
  "Saqlamasdan yopish": "Close without saving",
  "Saqlangan holat:": "Saved state:",
  "Saqlangan holatni yangilash": "Refresh saved state",
  "Saqlangan kanal tasdig‘i tekshirildi. Post qayta yuborilmadi.":
    "Saved channel confirmation was checked. The post was not resent.",
  "Saqlangan kuzatuv yo‘q. Bu nol natija degani emas.":
    "No saved observations. This does not mean zero results.",
  "Saqlangan kuzatuvlar": "Saved observations",
  "Saqlangan kuzatuvlar yuklanmoqda…": "Loading saved observations…",
  "Saqlangan oxirgi xato (": "Last saved error (",
  "Saqlangan post": "Saved post",
  "Saqlangan postingizni ochib, matn va ko‘rinishini tekshiring.":
    "Open your saved post and review its text and appearance.",
  "Saqlangan tarix": "Saved history",
  "Saqlangan versiya ko‘rinishi": "Saved revision preview",
  "Saqlangan yuborish talqini": "Saved delivery revision",
  Saqlanmagan: "Unsaved",
  "Saqlanmagan o‘zgarishlar": "Unsaved changes",
  "Saqlanmagan o‘zgarishlar bor. Chiqib ketilsinmi?":
    "You have unsaved changes. Leave anyway?",
  Saqlash: "Save",
  "Saqlash va mahsulot haqidagi savollarni kuzating.":
    "Track saves and questions about the product.",
  Saqlashlar: "Saves",
  Sarlavha: "Title",
  "Sarlavha, manzil yoki tavsif": "Title, URL or description",
  Sayt: "Site",
  "Sayt menyusi va /topics/… sahifalari.":
    "Site navigation and /topics/… pages.",
  "Sayt nomi": "Site name",
  "Sayt sozlamalari saqlandi.": "Site settings saved.",
  "saytda ochish": "open on site",
  "Saytni ochish": "Open site",
  Se: "Tue",
  "SEO sarlavha": "SEO title",
  "SEO sarlavha (ixtiyoriy)": "SEO title (optional)",
  "SEO tavsif": "SEO description",
  "SEO tavsif (ixtiyoriy)": "SEO description (optional)",
  "Server har 5 daqiqada tekshiradi. Katalogni yangilaydi va tasdiqlangan postlarni bo‘sh vaqtlarga rejalashtiradi — hech narsani o‘zi yubormaydi.":
    "The server checks every 5 minutes. It refreshes the catalogue and schedules approved posts into available slots; it does not send anything automatically.",
  "Server javobi kutilgan formatda emas. Qayta urinib ko'ring.":
    "Unexpected server response. Try again.",
  "Serverga ulanib bo'lmadi. Mahalliy xizmat ishlayotganini tekshiring.":
    "Unable to connect to the server. Check the local service is running.",
  Sesh: "Tue",
  Sh: "Sat",
  "Shablon tanlang": "Choose a template",
  "Shablon tanlash": "Select template",
  "Shablon va matnni tekshirish post yaratishning bir qismidir.":
    "Template and text review are part of post creation.",
  "Shablonlarni birga tanlang": "Choose templates together",
  "Shablonni shu yerda taqqoslang. Kundalik post uchun tanlangan variantni qo‘llang.":
    "Compare templates here. Apply the chosen variant to an everyday post.",
  Shan: "Sat",
  "Shoshilmasdan, muntazam.": "Steady, without rushing.",
  "Shu hafta": "This week",
  "Shu mahsulotdan boshlash": "Start with this product",
  "Shu post va tasvirlarni ko‘rsatilgan akkauntga belgilangan vaqtda yuborishga ruxsat beraman.":
    "I authorize sending this post and its images to the shown account at the scheduled time.",
  "Shu post varianti tekshirildi.": "This post variant was reviewed.",
  "Shu postning kuzatuv tarixi": "Observation history for this post",
  "Shu variant tekshirildi": "This variant was reviewed",
  "Siz tekshirgan matn": "Your reviewed text",
  "Slotni o‘chirish": "Delete slot",
  "So‘rovlar": "Enquiries",
  "so‘z": "words",
  "so‘z ·": "words ·",
  Sozlangan: "Configured",
  "Sozlangan xizmat hali tekshirilgan ulanish degani emas. Maxfiy kalitlar bu sahifada kiritilmaydi.":
    "Configured service does not mean a verified connection. Secret keys are not entered on this page.",
  "Statik sayt (ZIP)": "Static site (ZIP)",
  "ta hikoya. Nashr qilinganlari saytda, RSS va sitemap’da.":
    "stories. Published stories appear on the site, in RSS and the sitemap.",
  "ta mahsulot ish stolida. Katalogdagi faol mahsulotlar, narxlar va uch tildagi matn shu yerga keladi.":
    "products in the workspace. Active catalogue products, prices and text in three languages appear here.",
  "ta qoralama yaratish": "drafts to create",
  "ta tasdiqlangan post rejasiz turibdi.": "approved posts have no plan.",
  "ta yangi reja": "new plans",
  Tahlil: "Analysis",
  "Tahlil tili": "Analysis language",
  "Tahrir saqlanmagan. Sahifada qoling yoki saqlamasdan chiqing.":
    "Changes are unsaved. Stay on this page or leave without saving.",
  "tahririni yaratish": "create edition",
  "Tahrirlashni davom ettirish": "Continue editing",
  "Tahrirni bekor qilish": "Discard changes",
  Takliflar: "Suggestions",
  "Takliflar mahsulot ma’lumotlarini o‘z-o‘zidan o‘zgartirmaydi.":
    "Suggestions do not change product facts automatically.",
  "Taklifni ko‘rib chiqish": "Review suggestion",
  "Tanlangan tasvirlar jami 12 MiB dan oshmasin.":
    "Selected images must total no more than 12 MiB.",
  "Tanlangan tasvirlar so‘rov bilan xizmatga yuboriladi.":
    "Selected images will be sent to the service with the request.",
  "Tanlash kanalga ulanganlikni anglatmaydi.":
    "Selecting a channel does not connect it.",
  Taqvim: "Calendar",
  "Taqvimda ko‘rish": "View in calendar",
  Tarix: "History",
  "Tarjima qoralamasi yaratildi — matnni shu tilga o‘giring va nashr qiling.":
    "Translation draft created — translate the text into that language and publish it.",
  Tartib: "Order",
  "Tasdiq:": "Confirmation:",
  Tasdiqlangan: "Confirmed",
  "Tasdiqlangan ulanish orqali yuborish yoki qo‘lda eksport":
    "Send through a verified connection or export manually",
  "Tasdiqlangan, rejasiz": "Approved, unplanned",
  "Tashqi akkaunt tekshiruvi hozir mavjud emas. Sozlanganlik yuborishga ruxsat bermaydi.":
    "External account verification is unavailable. Configuration does not authorize delivery.",
  "tasi ko‘rsatilgan. Haftalik hisob to‘liq tarixdan olinadi.":
    "shown. Weekly totals use the full history.",
  "Tasvir bu kanalning hajm chegarasidan katta.":
    "This image exceeds the channel’s size limit.",
  "Tasvir joylashuvi": "Image placement",
  "Tasvir kutubxonaga qo‘shildi.": "Image added to the library.",
  "Tasvir manbasi": "Image source",
  "Tasvir qo‘shish": "Add image",
  "Tasvir so‘rovi navbatga qo‘shildi.": "Image request queued.",
  "Tasvir tahlili so‘rovi": "Image analysis request",
  "Tasvir tayyorlash": "Image preparation",
  "Tasvir tayyorlash navbatga qo‘shildi.": "Image preparation queued.",
  "Tasvir turi": "Image type",
  "Tasvir uchun topshiriq": "Image prompt",
  "Tasvir uchun topshiriq yozing.": "Write an image prompt.",
  "Tasvir yaratish": "Generate image",
  "Tasvir yaratish mahsuloti": "Product for image generation",
  "Tasvir yaratish modeli": "Image generation model",
  "Tasvir yaratish xizmati": "Image generation service",
  "Tasvirdan post g‘oyasi": "Post idea from an image",
  Tasvirlar: "Images",
  "Tasvirlarni tahlil qilish": "Analyse images",
  "Tasvirni tahlil qilish": "Analyse image",
  "Tasvirni tayyorlab, postga qo‘shing.":
    "Prepare the image and add it to the post.",
  "Tasvirni tayyorlash": "Prepare image",
  Tavsif: "Description",
  "Tavsif (": "Description (",
  Tayyor: "Ready",
  "Tayyor karta": "Prepared card",
  "Tayyor kartalardan jurnal": "Journal from prepared cards",
  "Tayyor postni tekshiring, so‘ng kanal va vaqtni tanlang.":
    "Review the prepared post, then choose a channel and time.",
  "Tayyor tasvir postga qo‘shildi.": "Prepared image added to the post.",
  "Tekshirildi deb belgilash": "Mark as reviewed",
  "Tekshirildi:": "Checked:",
  Tekshirilgan: "Verified",
  "Tekshirilgan matnni saqlash": "Save reviewed text",
  "Tekshirilgan post va manzil yuborish navbatiga saqlandi.":
    "Reviewed post and destination saved to the delivery queue.",
  "Tekshirilgan post, tasvir va kanal manzilini ko‘rib, alohida tasdiqlang.":
    "Review the approved post, image and channel destination, then confirm separately.",
  "Tekshirilgan postni rejangizga qo‘shing. Ulanmagan kanalda u reja sifatida saqlanadi.":
    "Add a reviewed post to your plan. On disconnected channels it remains a plan.",
  "Tekshirilgan tasvir tahlili matni": "Reviewed image analysis text",
  Tekshirilmagan: "Unverified",
  Tekshiring: "Review",
  "Tekshirish kerak": "Needs review",
  "Tekshirish kerak ·": "Needs review ·",
  "Tekshirish natijasi": "Review result",
  "Tekshirish uchun manba matni": "Source text to review",
  "Tekshirish va eksport aynan shu tasvirni ishlatadi.":
    "Review and export use this exact image.",
  "Tekshiruv ishlayapti": "Check in progress",
  "Tekshiruv o‘tmadi": "Verification failed",
  Tiklash: "Restore",
  Til: "Language",
  Tillar: "Languages",
  "Tipografik muqova ishlatiladi.": "A typographic cover will be used.",
  "To‘liq kiyilgan foto va qisqa sarlavha.":
    "Full outfit photo and a short heading.",
  "To‘xtatilgan": "Held",
  Topilmadi: "Not found",
  Topshiriq: "Prompt",
  "Toshkent vaqti.": "Tashkent time.",
  "Ulanish holatini ko‘rish": "View connection status",
  Ulanishlar: "Connections",
  "Ulanishlarni tekshirish": "Check connections",
  Ulanmagan: "Disconnected",
  "Ulanmagan kanal uchun saqlangan vaqt qo‘lda joylash rejasidir. Kanal keyinroq sozlansa ham, oldingi rejalar o‘z-o‘zidan yuborilmaydi.":
    "For disconnected channels, the saved time is a manual publishing plan. Connecting the channel later does not send earlier plans automatically.",
  "Ulanmagan kanal uchun ZIP paketni yuklab, qo‘lda joylash mumkin. Haqiqiy yuborish alohida sozlanadi va tasdiqlangan post talqinini ishlatadi.":
    "For disconnected channels, download a ZIP package and publish manually. Actual delivery is configured separately and uses the approved post revision.",
  Ulashishlar: "Shares",
  "Ushbu niyat qayta yoqilmaydi. Ulanishni tekshirib, yangi reja yarating.":
    "This request cannot be re-enabled. Check the connection and create a new plan.",
  "Ustki qator": "Top line",
  Vaqt: "Time",
  "Vaqt Toshkent bo‘yicha. Ulanmagan kanalda post reja sifatida saqlanadi.":
    "Times use Tashkent. On disconnected channels the post remains a plan.",
  "Vaqti o‘tgan rejalar": "Overdue plans",
  "Vaqtlar Toshkent bo‘yicha. “Rejada” holati post hali joylanmaganini bildiradi.":
    "Times use Tashkent. Planned means the post has not been published yet.",
  Vazifa: "Purpose",
  "Vazifasini belgilang": "Set its purpose",
  versiya: "version",
  "Vertikal siljish": "Vertical offset",
  "X so‘rovi kredit sarflashi mumkin. Faqat tugma bosilganda o‘qiladi.":
    "Reading X data may use credits. Data is read only when the button is pressed.",
  "Xabarni yopish": "Dismiss message",
  Xato: "Failed",
  "Xato, noma’lum yoki kechikkan tasdiqlangan yuborish yo‘q.":
    "No failed, unknown or overdue authorized deliveries.",
  Xizmat: "Service",
  "Xizmat holatini yangilash": "Refresh service status",
  "Xizmatdagi model nomi · ixtiyoriy": "Service model name · optional",
  "Xizmatni sozlang va aniq manba bilan topshiriq yuboring.":
    "Configure the service and submit a prompt with a clear source.",
  "Xuddi shu kompozitsiya to‘q fonda.":
    "The same composition on a dark background.",
  Ya: "Sun",
  Yak: "Sun",
  Yakun: "Conclusion",
  "Yakuniy savol": "Closing question",
  "yangi ·": "new ·",
  "Yangi hikoya": "New story",
  "Yangi o‘lchov qo‘shish": "Add a new measurement",
  "Yangi o‘zgarishlar saqlanmagan. Avval ularni saqlang.":
    "New changes are unsaved. Save them first.",
  "yangi oynada": "in a new window",
  "Yangi post": "New post",
  "Yangi signal yo‘q": "No new signals",
  "Yangi tasvir": "New image",
  "Yangi tasvir mahsulot shakli va matosiga mosligi bo‘yicha tekshiriladi. Tayyor post matni alohida qoladi.":
    "Review the generated image against the product’s shape and fabric. The prepared caption stays separate.",
  "yangilandi ·": "updated ·",
  "Yangilanmoqda…": "Refreshing…",
  "Yaqin reja": "Upcoming plan",
  "Yaqin rejalar": "Upcoming plans",
  "Yaratilgan tasvirlar hali yo‘q": "No generated images yet",
  "Yarim mato + mahsulot": "Half fabric + product",
  Yopish: "Close",
  Yoqilgan: "Enabled",
  Yoqtirishlar: "Likes",
  "Yorug‘ fon": "Light background",
  "Yozuv o‘zgarmas tarixga qo‘shiladi. Xatoni tuzatish uchun keyin yangi kuzatuv qo‘shing.":
    "The record enters immutable history. To correct an error, add a new observation later.",
  "yozuvdan eng so‘nggi": "latest records out of",
  Yuborilmoqda: "Sending",
  "Yuborish navbati": "Delivery queue",
  "Yuborish navbati bekor qilindi.": "Delivery queue item cancelled.",
  "Yuborish navbatiga qo‘shish": "Add to delivery queue",
  "Yuborish ruxsatlari": "Delivery permissions",
  "Yuborish tasviri PNG yoki JPEG bo‘lishi kerak.":
    "Delivery images must be PNG or JPEG.",
  "Yuborishga ruxsat berish": "Authorize delivery",
  "Yuborishni ko‘rib chiqish": "Review delivery",
  "Yuklanmoqda…": "Loading…",
  Yuklash: "Download",
  Yuqoriga: "Move up",
  "Natija noma’lum. Kanal tarixini qo‘lda tekshiring; shu amal qayta yuborilmaydi.":
    "Unknown outcome. Check the channel history manually; this operation will not be sent again.",
  yoqilgan: "Enabled",
  objectId: "Object ID",
  collection: "Collection",
  place: "Place",
  period: "Period",
  materials: "Materials",
  technique: "Technique",
  dimensions: "Dimensions",
  maker: "Maker",
  attributionNote: "Attribution note",
  kind: "Kind",
  credit: "Credit",
  rights: "Rights",
  sourceUrl: "Source URL",
  region: "Region",
  fabric: "Fabric",
  motif: "Motif",
  colour: "Colour",
  color: "Colour",
  front: "Front",
  detail: "Detail",
  reference: "Reference",
  generated: "Generated",
  hero: "Hero",
  draft: "Draft",
  published: "Published",
  scheduled: "Scheduled",
  archived: "Archived",
  planned: "Planned",
  accepted: "Accepted by channel",
  queued: "Queued",
  running: "Running",
  completed: "Completed",
  failed: "Failed",
  unknown: "Outcome unknown",
  cancelled: "Cancelled",
  new: "New",
  contacted: "Contacted",
  qualified: "Qualified",
  closed: "Closed",
  confirmed: "Confirmed",
  cumulative: "Cumulative",
  post: "Post",
  account: "Account",
  places: "Places",
  techniques: "Techniques",
  motifs: "Motifs",
  occasions: "Occasions",
  textileSlugs: "Related textiles",
  kirish: "Introduction",
};
export function translateUi(
  source: string,
  locale: UiLocale,
  variables?: Record<string, string | number>,
): string {
  // These envelopes come from legacy operator responses; captures stay verbatim.
  const patterns: [RegExp, string, string[]][] = [
    [
      /^Imported (\d+) drafts; preserved (\d+) existing items\.$/,
      "Imported {created} drafts; preserved {skipped} existing items.",
      ["created", "skipped"],
    ],
    [/^“(.*)” bo‘limi saqlandi\.$/, "Topic “{name}” saved.", ["name"]],
    [
      /^Amal bajarilmadi \((\d+)\)\. Qayta urinib ko'ring\.$/,
      "The operation failed ({status}). Try again.",
      ["status"],
    ],
    [
      /^Xizmat (\d+) xato qaytardi\.$/,
      "The service returned error {status}.",
      ["status"],
    ],
    [/^Rejalashtirildi: (.*)\.$/, "Scheduled: {date}.", ["date"]],
    [
      /^(\d+)-versiya tiklandi\.$/,
      "Revision {revision} restored.",
      ["revision"],
    ],
  ];
  for (const [pattern, message, names] of patterns) {
    const match = source.match(pattern);
    if (match)
      return translateUi(
        message,
        locale,
        Object.fromEntries(
          names.map((name, index) => [name, match[index + 1] ?? ""]),
        ),
      );
  }
  const key = aliases[source] ?? source;
  const translated = locale === "en" ? key : (uiMessages[key]?.[locale] ?? key);
  return translated.replace(/\{(\w+)\}/g, (match, name: string) =>
    variables && Object.hasOwn(variables, name)
      ? String(variables[name])
      : match,
  );
}
