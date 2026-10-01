import json, re
from datetime import datetime
from zoneinfo import ZoneInfo
from pathlib import Path
base = Path(__file__).parent
posts = []
def section(heading, *paragraphs, sourceIndexes=None, items=None):
    record={'heading':heading,'paragraphs':list(paragraphs)}
    if sourceIndexes is not None: record['sourceIndexes']=sourceIndexes
    if items: record['list']=items
    return record
def source(label,url,supports):
    return {'label':label,'url':url,'supports':supports}
def add(slug,title,excerpt,intro,format,promise,sections,takeaway,closing,sources):
    posts.append({'slug':slug,'topic':'cloth','title':title,'excerpt':excerpt,'intro':intro,'format':format,'readerPromise':promise,'sections':sections,'takeaway':takeaway,'closingQuestion':closing,'sources':sources})

add('pattern-placement-and-the-whole-piece',
    'Why an ikat edge can look out of focus',
    'Follow one wavering outline and a whole method of making begins to appear.',
    'A red shape meets a pale ground. From a distance, the border seems definite. Up close, it breaks into tiny steps and slender streaks. Your first instinct might be to adjust the focus. With an ikat textile, that little uncertainty can be the most interesting place to look. The edge invites a question: at what point did this pattern become part of the cloth?',
    'Close-looking explainer',
    'Recognise the difference between a visual effect and evidence of a textile technique.',
    [section('The pattern arrives before the cloth',
        'The Met describes ikat as a method of binding unwoven threads before dyeing them. The bindings create areas that resist the dye. When the threads are woven, those planned areas form the design. The pattern therefore begins in a set of threads, rather than as a picture applied to an already finished surface.', sourceIndexes=[0]),
     section('Look along the edge, not only at the motif',
        'That order of making changes the question you bring to the textile. Instead of asking whether someone drew a perfectly smooth curve, ask how many threads are contributing to it. Trace a short length of outline with your eyes. Does it seem to travel through a series of narrow colour changes? Does one line stay firm while another looks hazy?',
        'Seattle Art Museum’s ikat exhibition described the technique as resistant to exact linear design. Think of that as an invitation to look at a boundary for longer. Compare a few examples: how differently can an edge flicker, soften, or hold firm? The variety is part of what makes this small detail rewarding.', sourceIndexes=[1]),
     section('Try a two-distance reading',
        'Choose a museum image with a technique recorded as ikat. At full view, describe the arrangement in one sentence: perhaps vertical bands, a field of large shapes, or alternating areas of colour. Then enlarge one small boundary. Write a second sentence using only things you can actually see. Keep the two sentences beside each other.',
        'The first sentence might describe rhythm. The second might describe threads, transitions, or interruptions. Put them side by side and notice how your attention changes. A bold shape that seemed immediate can become a collection of tiny decisions. If individual threads remain out of view, choose another boundary that the photograph shows more clearly.'),
     section('Follow the edge, then follow the story',
        'A printed design can imitate a wavering edge, and a photograph can soften one too. For an object whose making interests you, follow the visual clue to its description. A museum’s medium field or a producer’s explanation can take you from the appearance of an edge to the process behind it.',
        'Sometimes the smallest irregularity is what holds your attention. The outline becomes less like a line around a shape and more like a place where several colours meet. Let your eyes travel along it once more. Which part feels restless, and which part gives you somewhere to settle?')],
    'Read the whole pattern for its rhythm, then one edge for its structure. Confirm the technique separately.',
    'Which boundary becomes more interesting when you move closer?',
    [source('The Met — Indonesian ikat','https://www.metmuseum.org/exhibitions/listings/2006/ikat-textiles','Binding and resist-dyeing threads before weaving.'),source('Seattle Art Museum — Ikat: A World of Compelling Cloth','https://www.seattleartmuseum.org/whats-on/exhibitions/ikat','An exhibition discussion of ikat and exact linear design; not a universal identification test.')])

add('when-does-pattern-enter-cloth',
    'When does a pattern enter a piece of cloth?',
    'Before weaving, during weaving, or afterwards: the same-looking flower can have different beginnings.',
    'Imagine three flowers that look almost identical in a photograph. One belongs to the structure of the fabric. Another was printed onto cloth. A third was stitched over a ground. Their colours might match, yet their making stories would begin at different moments. Asking when the pattern entered the cloth gives you a way into those stories without needing a cupboard full of technical words.',
    'Making timeline',
    'Ask a useful process question before assigning a technique from appearance.',
    [section('First, give the threads directions',
        'In the V&A’s textile glossary, warp means the threads held under tension on a loom, running along the length of a woven fabric. Weft means the threads interlaced across them. These names describe jobs and directions, not particular fibres. Silk or cotton does not tell you which thread is the warp.', sourceIndexes=[0]),
     section('Three moments, three possible stories',
        'A design can be incorporated as cloth is woven, or printed onto fabric that has already been woven. The V&A teaches these as two distinct ways of producing patterned textiles. An embroidered design introduces another set of questions about stitches and the ground beneath them. A finished object may combine several methods, so the timeline can have more than one entry.', sourceIndexes=[1]),
     section('Make a small map of one object',
        'Take a scarf, shirt, or cushion that you already have permission to examine. Draw three boxes on paper: yarn, cloth, finished object. Leave generous space between them. Write what you know in the relevant box. If the label gives a fibre, put it near yarn. If a maker identifies a print, put it beside cloth. If you can see a sewn-on decoration, note it near the finished object.',
        'Use a different mark for a guess. An empty box is useful too: perhaps you can name the colour you love but still wonder how it reached the fabric. Leave room beside the diagram for an answer. The map can grow during a conversation, with each new detail finding its own place.'),
     section('Ask what the close-up would need to show',
        'Now imagine showing the pattern to someone who has only a front-view photograph. What would you bring them closer to? A stitched petal might invite a detail view. A dense woven field might make you want to follow one crossing. Choose the next view according to the question that caught your interest.',
        'For an unfamiliar museum object, read the medium and technique fields. Return to the image with those words in mind. Sometimes a term suddenly gives a detail a shape; sometimes it raises a new question. Let the description and the image talk to each other rather than expecting either to do all the work.'),
     section('The timeline makes room for surprise',
        'The most satisfying discoveries may involve several boxes at once. A patterned fabric can become the ground for another pattern. A printed lining can sit inside a woven outer layer. The question changes from “What kind of pattern is this?” to “What happened, and in what order?” That second question leaves room for the work to be more complicated than its first impression.',
        'Keep your map. You might add a scrap of colour, a small sketch, or the word a maker used for one step. Soon “patterned fabric” will feel like the opening line of a story. You will have several ways to ask what happens next.')],
    'Fibre names the material; warp and weft name thread roles; the timeline asks how pattern appeared.',
    'At which stage does the object in front of you leave the biggest gap?',
    [source('V&A — An A–Z of Opus Anglicanum','https://www.vam.ac.uk/articles/a-z-opus-anglicanum','Definitions of warp and weft.'),source('V&A — An Introduction to Textile Techniques: Weaving & Printing','https://www.vam.ac.uk/event/K3M0rdMJkA7/p25119-an-introduction-to-textile-techniques','Distinguishes designs incorporated through weaving from designs printed onto woven cloth.')])

add('a-cloth-name-is-not-an-ingredients-list',
    'A cloth name is not an ingredients list',
    'A beautiful name starts a conversation. Fibre, technique, and place keep it precise.',
    'Someone offers a piece of cloth and gives you its name. The name may sound familiar enough that you stop asking questions. But what have you actually learned? Perhaps a tradition, a commercial description, a structure, or a family of textiles. You still may not know the fibres in this particular length. A useful description needs more than one line, and those lines do different work.',
    'Label field guide',
    'Separate a textile’s name from verified information about its composition and making.',
    [section('Let the name keep its context',
        'UNESCO’s account of the Crafts Development Center in Margilan places atlas and adras making within a specific safeguarding practice. It describes training and transmission, rather than offering a composition label for every textile sold under those names. That distinction lets a tradition retain its context without making a claim about an unidentified piece of cloth.', sourceIndexes=[0]),
     section('One line for material, another for method',
        'The Met records one nineteenth-century robe made in present-day Uzbekistan as silk and cotton, with an ikat-woven technique. The record gives material and method separately. It does not supply a percentage blend, and the two fibre names alone would not justify inventing one. A description can be informative while still leaving a question unanswered.', sourceIndexes=[1]),
     section('Try a four-line description',
        'Begin with the name as it was given to you, including the speaker or label. On a new line, record the stated fibre composition. On a third, write the documented technique. On the fourth, record the place of production if you know it. Do not let an empty line borrow its answer from a neighbouring one.',
        'A regional name and a production place may tell different parts of the story. A surface that looks silky may still leave you wondering about its fibres. Separate lines make room for both the associations you enjoy and the details you want to learn. You can begin with delight and stay curious.'),
     section('Make the question smaller',
        'Instead of asking “Is this real?” ask what the person means by the name they used. Instead of asking whether a cloth is good, ask which fibres are documented and how the surface was made. Smaller questions invite more useful answers. They also make room for a producer to describe a variation rather than defend a broad label.',
        'If two descriptions disagree, write down who supplied each one. You could ask for a composition label, a production explanation, or the place where this particular cloth was made. Add your own notes about shine or feel on a separate line. Those impressions make the entry yours without having to answer every technical question.'),
     section('Keep a glossary with blank spaces',
        'A personal textile glossary can become a collection of encounters rather than a list of absolute definitions. Beside each name, save one object record, one question, and one detail you noticed. Add dates when you return to the entry. The blank spaces show how much the word has yet to teach you.',
        'This approach also makes a conversation kinder. You are asking someone to explain their cloth, not requiring them to fit it into your first idea of the category. A name can remain rich with associations while the description of the particular object remains exact.')],
    'Record name, composition, technique, and production place separately. Let missing information remain visible.',
    'Which familiar textile name have you never asked anyone to explain?',
    [source('UNESCO — Margilan crafts','https://www.unesco.org/en/articles/crafts-development-center-margilan','Safeguarding, training, and transmission; not item-level fibre evidence.'),source('The Met — Robe 1975.388.1','https://www.metmuseum.org/art/collection/search/85503','Date, geographic attribution, silk/cotton, and ikat; no percentages.')])

add('two-patterns-sharing-one-cloth',
    'Two patterns can share one cloth',
    'A woven band and an embroidered shape may sit together while following different rules.',
    'It is tempting to name an entire textile from its most noticeable detail. A shining shape catches your eye and the whole object becomes “embroidery.” A strong band appears and the whole surface becomes “woven pattern.” What happens if you look for the boundary between the two? One textile can hold several processes, and finding where one gives way to another is like finding a second voice in a song.',
    'Technique comparison',
    'Notice layered methods without treating different textile traditions as interchangeable.',
    [section('A specific example, in a specific place',
        'The Met’s Woman’s Ceremonial Skirt, or tapis, is attributed to a Lampung artist in Sumatra. Its record describes warp-ikat bands alternating with embroidered panels. This is a Lampung textile story. Within that setting, the movement between two methods gives us a fascinating junction to follow.', sourceIndexes=[0]),
     section('Where does each pattern depend on the ground?',
        'The V&A’s embroidery guide distinguishes counted-thread work, which follows counted threads or sections of a foundation, from freestyle work that is not constrained in that way by the underlying weave. Embroidery itself therefore contains different relationships between pattern and ground. A stitched line need not follow the same logic as its neighbour.', sourceIndexes=[1]),
     section('Describe a junction',
        'Choose one museum image whose record identifies more than one technique. Look for a small area where a patterned zone meets another. Describe what the junction does before naming either method. Is there a narrow dividing line? Does one shape end abruptly? Does a plain area give the two patterns room?',
        'Use simple spatial words: above, beside, through, between. They turn a complicated surface into something you can follow. A rough sketch might show two patterns pulling in different directions, or a narrow band holding them together. If the junction stays indistinct, pick another area the image reveals more clearly.'),
     section('The reverse opens another view',
        'With an everyday object you own, and if it can be turned safely, compare the two sides. Notice where a thread takes a different path or where more of the ground appears. Does the reverse feel busier, quieter, or simply unfamiliar? Keep the two views beside each other in your notes.',
        'For a museum object, explore the views the collection supplies. A detail or an angled photograph may reveal a relationship that the full view hid. Make a second sketch. Leave a little space around it for the part you would like to see next.'),
     section('Two voices change the whole reading',
        'Return to the full view. Which pattern establishes the rhythm for your eye, and which interrupts it? Ask another viewer to choose too. Point to the places that led each of you there. A disagreement can uncover a line or patch of ground that one of you walked straight past.',
        'The textile now has more than one voice. You can hear a steady sequence in one area and something freer beside it. Keep the recorded methods with your notes, then add the small junction that made you stop. It may become the detail you remember long after the whole image fades.')],
    'Identify documented methods separately, then study the place where their patterns meet.',
    'Which small junction changes your reading of the whole textile?',
    [source('The Met — Woman’s Ceremonial Skirt (Tapis), object 1988.104.1','https://www.metmuseum.org/art/collection/search/315915','Lampung attribution and combination of warp-ikat bands with embroidered panels.'),source('V&A — Embroidery styles: an illustrated guide','https://www.vam.ac.uk/articles/embroidery-styles-an-illustrated-guide','Counted-thread and freestyle embroidery relate differently to foundation cloth.')])

add('what-a-seam-does-to-a-repeat',
    'What a seam does to a repeat',
    'A paper experiment reveals why the same pattern can become three different compositions.',
    'Draw a row of circles. Draw another beneath it. You now have a pattern, but you do not yet have a garment. Cut that sheet into two pieces and move one slightly. A circle disappears. Two halves meet. A new gap opens. Nothing has changed in the drawing, yet the composition has changed completely. That is a useful way to think about the moment cloth becomes pieces that must join.',
    'Paper studio exercise',
    'See how cutting and joining affect a pattern without claiming undocumented production choices.',
    [section('Set up the experiment',
        'Take paper, a pencil, and scissors. Draw a simple repeating shape across a sheet. Keep the pattern easy enough that you can recognise what happens when part of it disappears. Circles, short bars, or a repeated letter will do. Make two more copies by hand; tiny differences will not prevent the exercise from working.',
        'On each copy, draw the same two rough garment panels. Think of their outlines as viewing windows. Keep them uncomplicated so a curved neckline or an elaborate sleeve does not steal the experiment. You are giving the pattern three chances to meet the same edge.'),
     section('Try three joins',
        'For the first pair, arrange the panels so one motif continues across the join. For the second, deliberately shift a panel until the motif breaks. For the third, put a plain strip of paper between them. Keep the outer outline as similar as you can. Otherwise a change in silhouette may hide what the join is doing.',
        'Move all three versions to arm’s length. Which join nearly disappears? Which becomes a line you notice immediately? Which gives your eyes a pause? No version is automatically the correct one. The exercise gives you three visible effects that you can describe and compare.'),
     section('A garment may contain several cloth stories',
        'LACMA’s record for a late nineteenth-century chapan lists silk-and-cotton plain weave, additional worked details, and lining materials that include printed cotton. The record is a reminder that “the fabric” may be an incomplete description of one garment. Its several components deserve separate attention rather than one assumed production story.', sourceIndexes=[0]),
     section('Bring the experiment back to an image',
        'Now look at a garment photograph. Locate one join that you can actually see. Follow a motif towards it and describe what happens. Does the shape continue, end, or change direction? Does a border create a new division? Keep your description limited to that view. A concealed seam cannot be evaluated from its invisible outline.',
        'If a join catches your eye, give its effect a name of your own: a stumble, a pause, a sudden turn. The maker’s intention may remain unknown, but your response can be precise. Describe the broken circle, the shifted rhythm, or the line of plain colour that caused it.'),
     section('Keep the surprising version',
        'You may prefer the arrangement that initially seemed wrong. Perhaps the broken repeat feels lively, while the continuous one feels calm. Ask someone else to choose a version and explain where their eye goes. Label all three with those responses. A small paper experiment can hold several opinions at once.',
        'The next time you look at patterned clothing, try reading the joins before the centre. The motif may be familiar, but the interruption can be new. A seam becomes part of the visual composition, not something your eye must always ignore.')],
    'Compare continuation, interruption, and a plain division before deciding which visual effect you prefer.',
    'Which of your three joins makes the pattern feel most alive?',
    [source('LACMA — Chapan M.2022.270.2','https://collections.lacma.org/object/246745','Several textile components, including printed-cotton lining.')])

(base/'posts.json').write_text(json.dumps(posts,ensure_ascii=False,indent=2)+'\n')

add('the-museum-label-has-a-missing-person',
    'The museum label has a missing person',
    'An object number can be exact while the maker’s name remains unknown.',
    'You can learn the date of a robe, its materials, its length, and the name of the collector who gave it to a museum. Then you look for the person who made it. There may be no name. The record feels full until that absence becomes visible. Reading an object label carefully means paying attention to what it tells you, and to the person it cannot yet identify.',
    'Object-record detective story',
    'Read certainty, uncertainty, and attribution as different kinds of information.',
    [section('Begin with an actual gap',
        'LACMA’s late nineteenth-century Robe (Chapan) is attributed to an unknown maker. Its place is recorded as Central Asia, probably Uzbekistan. The collection record supplies an accession number and a donor credit. Those are useful facts, but neither supplies the missing maker’s identity. “Probably” also belongs to the place attribution and should travel with it whenever the record is summarised.', sourceIndexes=[0]),
     section('The number answers a different question',
        'An accession number brings you back to the museum’s particular object. “Striped robe” might send you towards several garments; a number keeps you with this one. A credit line introduces another part of its journey into the collection. The maker’s name can remain absent even while those later details are precise.',
        'That unevenness gives you something to think about. The description might bring you close to the material while leaving the workshop distant. Who designed, dyed, wove, cut, or joined its components? Several kinds of work can sit behind the finished outline, even when the page cannot yet give them names.'),
     section('Make two columns, not a complete biography',
        'In one column, copy the facts that the record states. Preserve words such as “probably,” “about,” and “unknown.” In the other column, write the questions the facts raise. Who made the outer fabric? Was the garment assembled in the same place? What happened before it reached the recorded owner? Leave those sentences as questions.',
        'Some questions will stay open. Circle the one that makes you most curious, perhaps the place where the fabric became a garment. You can return to it as you find other records. The gap becomes a direction for looking, rather than a blank you have to fill immediately.'),
     section('Notice whose names are easy to retain',
        'When sharing an object, readers often keep its attractive title and omit the less tidy qualifiers. Try doing the reverse for a moment. Begin your note with the uncertainty, then add the facts. “The maker is unknown; this is the robe recorded as…” changes the emphasis without changing the evidence.',
        'The page may not explain why some names survived and others did not. Even so, noticing the imbalance changes how you describe the robe. Give the maker’s absence a place in your note, alongside the material, the date, and the route into the collection.'),
     section('Leave a trail someone can follow',
        'Save the object link and the date you read it. If you make a sketch, include the accession number on the page. A specific reference keeps your observation attached to a specific object. It also gives you a way to revisit the record if new research is added.',
        'The unfinished part of a label can stay with you. You may remember the colour first and the missing name later. Keep both. A robe can be admired as an object and read as a history that still has room for someone to reappear.')],
    'Retain the maker’s absence and the record’s qualifiers. Precision includes knowing which facts remain uncertain.',
    'Whose name appears in the record, and whose work remains unnamed?',
    [source('LACMA — Chapan M.2022.270.2','https://collections.lacma.org/object/246745','Unknown maker and probable Uzbekistan attribution.')])

add('one-robe-is-not-a-whole-region',
    'One robe is not a whole region',
    'How to enjoy a historical garment without turning it into a universal costume.',
    'A striking robe appears in your feed. Below it, someone writes “traditional Central Asian clothing.” The caption may help you find an unfamiliar object, yet it also compresses a large space and many lives into one image. Before the robe becomes your idea of how everybody dressed, give it back its particular record. The smaller story is usually more interesting than the sweeping one.',
    'Historical reading note',
    'Keep an object’s documented date, place, and materials attached to its image.',
    [section('Start at the scale of one object',
        'The Met’s robe numbered 1975.388.1 is dated to the nineteenth century and recorded as made in present-day Uzbekistan, in silk and cotton with an ikat-woven technique. Hold those details beside the image: one garment, a recorded time, and a particular geographic attribution.', sourceIndexes=[0]),
     section('Make the caption smaller',
        'Try a caption using the object’s title, date, place wording, and reference number. The big regional label becomes a smaller doorway. Someone else can now find this same garment and begin at the same place you did. You have given the image a home rather than only a mood.',
        'Add one detail you notice. Perhaps a repeated shape changes direction at a visible join, or a border creates a strong line. Let that small observation sit beside the historical information. Questions about occasion, identity, or rank can follow when a source for this object supplies more of its story.'),
     section('A shared technique does not mean a shared meaning',
        'The Met’s exhibition on Indonesian ikat emphasised several distinctive regional traditions and varied forms and uses. A common technique can lead into very different local stories. Follow the connection, then notice where the paths separate: another kind of garment, another setting, another reason for cloth to matter.', sourceIndexes=[1]),
     section('Compare by asking, not merging',
        'Open two object records from different places. Ask the same small questions of each: What is the title? How specific is the date? Is a maker named? What medium is recorded? What does the page actually say about use? The matching questions give you a comparison without forcing matching answers.',
        'One page may have a long account of ceremony while another has only a short description. Put the two beside each other anyway. What would you like to ask the quieter record? Its missing detail can become the reason you continue towards a third object.'),
     section('Leave enough room for difference',
        'There is still room for delight. You can love a colour relationship, notice a powerful silhouette, or wonder how a wearer moved. Write that response as yours. A historical object can make you imagine movement even while its recorded story remains incomplete.',
        'Keep links and object details beside the images in your reference collection. Group them around a question: openings, borders, sleeve shapes, or ways patterns meet. You might discover an unexpected visual neighbour. Follow it to its own record and see how the resemblance changes when you learn a little more.',
        'When a broad caption catches your attention, follow it to a particular garment. Start there, with one edge or one colour, and let the recorded details widen the view. A single robe can open several doors. You can enjoy each one without asking the robe to tell the whole story at once.')],
    'Use a particular object as an entry into history, preserving its date, attribution, and limits.',
    'Which detail becomes clearer when you replace a broad caption with a specific record?',
    [source('The Met — Robe 1975.388.1','https://www.metmuseum.org/art/collection/search/85503','Recorded date, present-day Uzbekistan, silk/cotton, and ikat.'),source('The Met — Indonesian ikat','https://www.metmuseum.org/exhibitions/listings/2006/ikat-textiles','Distinct regional traditions and varied forms and uses.')])

add('read-the-care-label-as-a-sequence',
    'The five small signs that slow you down',
    'Care labels become easier to read when you stop treating them as five unrelated pictures.',
    'You recognise the little iron. The triangle is less familiar. The circle looks important, but you cannot remember what it means. It is easy to turn a care label into a guessing game, especially when you are holding a new garment and thinking about everything except laundry. Reading the row as a sequence gives those small marks a place, and turns a vague impression into specific questions.',
    'Care-label decoding guide',
    'Understand the care categories and know when the individual label needs clarification.',
    [section('Give each sign its job',
        'GINETEX’s current guide groups textile care into washing, bleaching, drying, ironing, and professional care. The wash-tub number indicates a maximum washing temperature; bars underneath specify milder mechanical treatment. A triangle concerns bleaching, a square drying, an iron ironing, and a circle professional cleaning. The shapes form a set of instructions, rather than interchangeable signs for being careful.', sourceIndexes=[0]),
     section('The fibre label answers another question',
        'GINETEX explains that care labelling concerns the permitted treatment of the article, including colouring, finishing, accessories, and applications. A composition name alone therefore does not replace the care instructions for the finished garment. The label describes treatment limits; it is not a promise that every stain will disappear.', sourceIndexes=[1]),
     section('Read one garment, in one sitting',
        'Find the actual label on a garment you own. Photograph it clearly enough to see every small bar, dot, letter, and cross. Read from the first category to the last, writing a plain-language note for each. Use the official guide rather than a remembered chart that may omit a variation.',
        'Stop where you become uncertain. Is the square’s interior unclear? Has part of a symbol worn away? Does a written instruction conflict with the way you interpreted a mark? The aim is not to demonstrate fluency. It is to notice the exact place where a guess might turn into an unwanted experiment.'),
     section('Turn uncertainty into a precise request',
        'If you need clarification, send the producer or a qualified cleaner the full label and identify the garment. Ask about the unclear instruction itself. “What does this mark require for this piece?” is more useful than “Can silk be washed?” because it keeps the question attached to the finished article.',
        'An unclear or missing label leaves a practical question. Keep the garment details you have and ask for its specific care information before choosing a treatment. You can add the answer beside the label photograph when it arrives, making the next reading easier.'),
     section('Make a note you will actually find again',
        'A clear photo in a small care folder can be more useful than memorising every variation. Give it a name you will recognise later: the blue shirt, the patterned scarf, the jacket with the unusual trim. Include the producer’s clarification if you receive one, so the question does not have to be answered again.',
        'The next time you read a label, begin with the sequence and work through it. A small row of marks becomes five readable questions. The familiar iron has company now: the square, triangle, circle, and tub each have something particular to tell you.')],
    'Read the full care row for the individual article. Clarify an unclear mark before choosing treatment.',
    'Which category on your most-worn garment have you never checked closely?',
    [source('GINETEX — Care symbols under ISO 3758:2023','https://www.ginetex.net/GB/labelling/care-symbols.asp','Five care categories; washing-temperature maximum; bars for milder action; basic shape meanings.'),source('GINETEX — Care labelling','https://www.ginetex.net/labelling/','Treatment applies to the finished article including dyes, finishes, accessories, and applications; stain removal is not guaranteed.')])

add('five-minutes-with-one-piece-of-cloth',
    'Five minutes with one piece of cloth',
    'A short looking game for anyone who usually reads the label before seeing the object.',
    'You open a museum collection page, read three lines, and scroll on. You now know something about the object but may remember almost nothing you saw. Try reversing the order for five minutes. Look first, keep a small note, and consult the record afterwards. The exercise is deliberately short. It makes room for one encounter instead of asking you to become an expert before you begin.',
    'Timed museum-looking game',
    'Distinguish a visual observation from a fact learned through a collection record.',
    [section('Minute one: stay with the whole',
        'Choose one clear collection image. In the V&A’s phulkari section, you will find textiles associated with the Panjab, encompassing parts of northern India and eastern Pakistan. Pick one labelled image and give it the whole five minutes. Keep the page open, but save its description for minute four.',
        'Write three visual words without explaining them. They might concern colour, shape, spacing, or direction. Avoid “beautiful” for a moment, because it can conclude the encounter before you have described it. “Dense,” “diagonal,” or “interrupted” gives you somewhere to look again.', sourceIndexes=[0]),
     section('Minute two: find the quietest area',
        'Instead of enlarging the detail that first caught your eye, find the area with the least happening. How much space does it occupy? Does it surround a motif, separate two zones, or make a border easier to see? A quieter area can change the force of everything near it.',
        'If the pattern stays dense throughout, follow that density. Does one area feel more packed than another? Can a change in colour create breathing space even where there is little bare ground? A quiet area might be something your eye discovers rather than an obvious empty patch.'),
     section('Minute three: follow one path',
        'Choose a visible line, repeated shape, or colour. Follow it until it stops, changes, or leaves the picture. Sketch the route with a pencil. A rough diagram is enough. The sketch may show that your eye skipped a turn or assumed a continuation that the image never supplied.',
        'Simple descriptions work well here. “Short diagonal marks” gives you a line to follow; a small zigzag sketch gives you another. Save any unfamiliar stitch names for the next minute. For now, enjoy finding words that make the surface clearer to you.'),
     section('Minute four: let the record enter',
        'Now read the material and technique information. The V&A explains phulkari as darning-stitch work from the reverse of handwoven cotton, using silk thread. Compare that account with your note. Which part of the process can you actually locate in the supplied view? Which part is information you learned from the museum?',
        'Put an O beside observations and an R beside facts from the record. If you guessed something incorrectly, cross it out without erasing it. The change shows how evidence improved your first impression.', sourceIndexes=[0]),
     section('Minute five: carry away one question',
        'Finish with a question about the detail that stayed with you. Perhaps you want a reverse view or a closer look at the boundary you sketched. “What happens at this edge?” has somewhere to go. Put the question at the foot of your page like a small invitation to return.',
        'Save the link with your note. When you return, begin with your sketch rather than the label. Five minutes will not reveal the whole history of a textile. It can leave you with a remembered detail and a reason to look again, which is more than a page you have already forgotten.')],
    'Look, describe, check, and return. Keep what you saw distinct from what the record taught you.',
    'What did your sketch notice that your first three words missed?',
    [source('V&A — Indian embroidery, phulkari section','https://www.vam.ac.uk/articles/indian-embroidery','Panjab context, reverse-side darning stitch, handwoven cotton ground, and silk embroidery thread.')])

add('what-does-handmade-actually-tell-you',
    'What does “handmade” actually tell you?',
    'One word can hide many kinds of work. Better questions make the people and steps visible.',
    'A label says handmade. You picture a person at a table, beginning and finishing the whole thing. That picture may be comforting, but the word alone does not tell you which hands did which work. Thread preparation, dyeing, weaving, stitching, and finishing are different stages. Asking about them separately can make a textile story more human, because it gives more people a place in it.',
    'Claim-reading notebook',
    'Replace a broad making claim with questions about specific steps and evidence.',
    [section('Begin with work that can be named',
        'UNESCO’s account of the Margilan Crafts Development Center describes practical and theoretical training, local knowledge transmission, and workshops across several crafts. Its safeguarding work includes atlas and adras making. Here, a textile story unfolds through people learning skills and passing them on, as well as through finished cloth.', sourceIndexes=[0]),
     section('Make the word unfold',
        'Write handmade in the middle of a page. Around it, place the stages you want to ask about: preparing yarn, creating colour, making cloth, cutting, joining, and finishing. These are prompts, not a claim that every textile follows exactly this sequence. Add a space for the people involved at each stage.',
        'Which part did the description actually identify? Perhaps hand embroidery is specified while the ground fabric’s production is unexplained. Perhaps handweaving is documented while the garment’s assembly is not. Mark the supported step and leave the others open. The word becomes more informative when you resist extending it automatically.'),
     section('Ask for a process, not a performance',
        'A useful question is “Which stages are done by hand in this piece?” Another is “Who makes the cloth before it reaches the garment maker?” The answers may reveal several workers or suppliers. That is a fuller account, not a reason to prefer a solitary-maker story.',
        'A short process video may make you want to pause on one action: fingers arranging thread, a colour meeting cloth, a repeated movement finding its rhythm. Ask which piece or batch the film follows. The answer can keep the moment attached to the textile whose story you are reading.'),
     section('Leave praise attached to something visible',
        'You may admire the spacing of stitches or the way a border is resolved. Name the detail that held your attention. A small, precise compliment can make the work visible: this turn, this join, this patient sequence. It gives the person you are speaking to something particular to explain.',
        'Other questions may still interest you: fibre composition, working conditions, environmental impact, or how an item wears over time. Ask them in their own right. Handmade describes a kind of work; those further answers can bring more dimensions to the story when the producer has information to share.'),
     section('The people should survive the summary',
        'When you retell a textile story, keep the names and roles the producer has shared with permission. A weaver, a garment maker, and a workshop can each have a place. If someone remains unidentified, leave room beside their stage. The work is still there to be noticed.',
        'A memorable account can explain one difficult step, show how another depends on it, and introduce the people it can name. You leave able to picture work happening, with a better question for the next textile. The word handmade becomes the beginning of a conversation you would like to continue.')],
    'Ask which work, by whom, on which item. Keep broader claims tied to their own evidence.',
    'Which stage would you most like a textile story to show you?',
    [source('UNESCO — Margilan crafts','https://www.unesco.org/en/articles/crafts-development-center-margilan','Atlas/adras safeguarding, training, and craft workshops; no Ocharo production connection.')])

(base/'posts.json').write_text(json.dumps(posts,ensure_ascii=False,indent=2)+'\n')
for post in posts:
    body=[f"# {post['title']}","",datetime.now(ZoneInfo("Asia/Tashkent")).strftime("*Last updated: %Y-%m-%d %I:%M %p*"),"",f"Topic: Cloth & craft · Format: {post['format']}","",post['intro'],""]
    for item in post['sections']:
        body += [f"## {item['heading']}",""]
        for para in item['paragraphs']:
            body += [para,""]
        if item.get('list'):
            body += [f"- {x}" for x in item['list']]+[""]
        if item.get('sourceIndexes'):
            links=', '.join(f"[{post['sources'][i]['label']}]({post['sources'][i]['url']})" for i in item['sourceIndexes'])
            body += [f"Source for the documented facts in this section: {links}.",""]
    body += ["## Keep this", "",post['takeaway'],"",post['closingQuestion'],"","## Sources"," "]
    for i,src in enumerate(post['sources']):
        body += [f"{i+1}. [{src['label']}]({src['url']}) — {src['supports']}"]
    (base/f"och-post-{post['slug']}.md").write_text('\n'.join(body)+'\n')
counts=[]
for post in posts:
    text=' '.join([post['intro']]+[s for sec in post['sections'] for s in sec['paragraphs']]+[x for sec in post['sections'] for x in sec.get('list',[])]+[post['takeaway'],post['closingQuestion']])
    count=len(text.split())
    counts.append({'slug':post['slug'],'words':count})
assert len(posts)==10
assert len({x['slug'] for x in posts})==10
assert all(400<=x['words']<=650 for x in counts),counts
print(json.dumps(counts,indent=2))
