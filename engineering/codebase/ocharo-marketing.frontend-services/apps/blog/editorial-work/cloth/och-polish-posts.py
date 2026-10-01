from pathlib import Path
import json
base=Path(__file__).parent
posts=json.loads((base/'posts.json').read_text())
def change(p,s,i,text): posts[p]['sections'][s]['paragraphs'][i]=text
posts[0]['sections'][3]['heading']='Follow the edge, then follow the story'
change(0,1,1,'Seattle Art Museum’s ikat exhibition described the technique as resistant to exact linear design. Think of that as an invitation to look at a boundary for longer. Compare a few examples: how differently can an edge flicker, soften, or hold firm? The variety is part of what makes this small detail rewarding.')
change(0,2,1,'The first sentence might describe rhythm. The second might describe threads, transitions, or interruptions. Put them side by side and notice how your attention changes. A bold shape that seemed immediate can become a collection of tiny decisions. If individual threads remain out of view, choose another boundary that the photograph shows more clearly.')
change(0,3,0,'A printed design can imitate a wavering edge, and a photograph can soften one too. For an object whose making interests you, follow the visual clue to its description. A museum’s medium field or a producer’s explanation can take you from the appearance of an edge to the process behind it.')
change(0,3,1,'Sometimes the smallest irregularity is what holds your attention. The outline becomes less like a line around a shape and more like a place where several colours meet. Let your eyes travel along it once more. Which part feels restless, and which part gives you somewhere to settle?')

change(1,2,1,'Use a different mark for a guess. An empty box is useful too: perhaps you can name the colour you love but still wonder how it reached the fabric. Leave room beside the diagram for an answer. The map can grow during a conversation, with each new detail finding its own place.')
change(1,3,0,'Now imagine showing the pattern to someone who has only a front-view photograph. What would you bring them closer to? A stitched petal might invite a detail view. A dense woven field might make you want to follow one crossing. Choose the next view according to the question that caught your interest.')
change(1,3,1,'For an unfamiliar museum object, read the medium and technique fields. Return to the image with those words in mind. Sometimes a term suddenly gives a detail a shape; sometimes it raises a new question. Let the description and the image talk to each other rather than expecting either to do all the work.')
change(1,4,1,'Keep your map. You might add a scrap of colour, a small sketch, or the word a maker used for one step. Soon “patterned fabric” will feel like the opening line of a story. You will have several ways to ask what happens next.')

change(2,2,1,'A regional name and a production place may tell different parts of the story. A surface that looks silky may still leave you wondering about its fibres. Separate lines make room for both the associations you enjoy and the details you want to learn. You can begin with delight and stay curious.')
change(2,3,1,'If two descriptions disagree, write down who supplied each one. You could ask for a composition label, a production explanation, or the place where this particular cloth was made. Add your own notes about shine or feel on a separate line. Those impressions make the entry yours without having to answer every technical question.')

change(3,0,0,'The Met’s Woman’s Ceremonial Skirt, or tapis, is attributed to a Lampung artist in Sumatra. Its record describes warp-ikat bands alternating with embroidered panels. This is a Lampung textile story. Within that setting, the movement between two methods gives us a fascinating junction to follow.')
posts[3]['sections'][3]['heading']='The reverse opens another view'
change(3,2,1,'Use simple spatial words: above, beside, through, between. They turn a complicated surface into something you can follow. A rough sketch might show two patterns pulling in different directions, or a narrow band holding them together. If the junction stays indistinct, pick another area the image reveals more clearly.')
change(3,3,0,'With an everyday object you own, and if it can be turned safely, compare the two sides. Notice where a thread takes a different path or where more of the ground appears. Does the reverse feel busier, quieter, or simply unfamiliar? Keep the two views beside each other in your notes.')
change(3,3,1,'For a museum object, explore the views the collection supplies. A detail or an angled photograph may reveal a relationship that the full view hid. Make a second sketch. Leave a little space around it for the part you would like to see next.')
change(3,4,0,'Return to the full view. Which pattern establishes the rhythm for your eye, and which interrupts it? Ask another viewer to choose too. Point to the places that led each of you there. A disagreement can uncover a line or patch of ground that one of you walked straight past.')
change(3,4,1,'The textile now has more than one voice. You can hear a steady sequence in one area and something freer beside it. Keep the recorded methods with your notes, then add the small junction that made you stop. It may become the detail you remember long after the whole image fades.')

change(4,0,1,'On each copy, draw the same two rough garment panels. Think of their outlines as viewing windows. Keep them uncomplicated so a curved neckline or an elaborate sleeve does not steal the experiment. You are giving the pattern three chances to meet the same edge.')
change(4,3,1,'If a join catches your eye, give its effect a name of your own: a stumble, a pause, a sudden turn. The maker’s intention may remain unknown, but your response can be precise. Describe the broken circle, the shifted rhythm, or the line of plain colour that caused it.')
change(4,4,0,'You may prefer the arrangement that initially seemed wrong. Perhaps the broken repeat feels lively, while the continuous one feels calm. Ask someone else to choose a version and explain where their eye goes. Label all three with those responses. A small paper experiment can hold several opinions at once.')

change(5,1,0,'An accession number brings you back to the museum’s particular object. “Striped robe” might send you towards several garments; a number keeps you with this one. A credit line introduces another part of its journey into the collection. The maker’s name can remain absent even while those later details are precise.')
change(5,1,1,'That unevenness gives you something to think about. The description might bring you close to the material while leaving the workshop distant. Who designed, dyed, wove, cut, or joined its components? Several kinds of work can sit behind the finished outline, even when the page cannot yet give them names.')
change(5,2,1,'Some questions will stay open. Circle the one that makes you most curious, perhaps the place where the fabric became a garment. You can return to it as you find other records. The gap becomes a direction for looking, rather than a blank you have to fill immediately.')
change(5,3,1,'The page may not explain why some names survived and others did not. Even so, noticing the imbalance changes how you describe the robe. Give the maker’s absence a place in your note, alongside the material, the date, and the route into the collection.')
change(5,4,1,'The unfinished part of a label can stay with you. You may remember the colour first and the missing name later. Keep both. A robe can be admired as an object and read as a history that still has room for someone to reappear.')

change(6,0,0,'The Met’s robe numbered 1975.388.1 is dated to the nineteenth century and recorded as made in present-day Uzbekistan, in silk and cotton with an ikat-woven technique. Hold those details beside the image: one garment, a recorded time, and a particular geographic attribution.')
change(6,1,0,'Try a caption using the object’s title, date, place wording, and reference number. The big regional label becomes a smaller doorway. Someone else can now find this same garment and begin at the same place you did. You have given the image a home rather than only a mood.')
change(6,1,1,'Add one detail you notice. Perhaps a repeated shape changes direction at a visible join, or a border creates a strong line. Let that small observation sit beside the historical information. Questions about occasion, identity, or rank can follow when a source for this object supplies more of its story.')
change(6,2,0,'The Met’s exhibition on Indonesian ikat emphasised several distinctive regional traditions and varied forms and uses. A common technique can lead into very different local stories. Follow the connection, then notice where the paths separate: another kind of garment, another setting, another reason for cloth to matter.')
change(6,3,1,'One page may have a long account of ceremony while another has only a short description. Put the two beside each other anyway. What would you like to ask the quieter record? Its missing detail can become the reason you continue towards a third object.')
change(6,4,0,'There is still room for delight. You can love a colour relationship, notice a powerful silhouette, or wonder how a wearer moved. Write that response as yours. A historical object can make you imagine movement even while its recorded story remains incomplete.')
change(6,4,1,'Keep links and object details beside the images in your reference collection. Group them around a question: openings, borders, sleeve shapes, or ways patterns meet. You might discover an unexpected visual neighbour. Follow it to its own record and see how the resemblance changes when you learn a little more.')
change(6,4,2,'When a broad caption catches your attention, follow it to a particular garment. Start there, with one edge or one colour, and let the recorded details widen the view. A single robe can open several doors. You can enjoy each one without asking the robe to tell the whole story at once.')

change(7,3,1,'An unclear or missing label leaves a practical question. Keep the garment details you have and ask for its specific care information before choosing a treatment. You can add the answer beside the label photograph when it arrives, making the next reading easier.')
change(7,4,1,'The next time you read a label, begin with the sequence and work through it. A small row of marks becomes five readable questions. The familiar iron has company now: the square, triangle, circle, and tub each have something particular to tell you.')

change(8,0,0,'Choose one clear collection image. In the V&A’s phulkari section, you will find textiles associated with the Panjab, encompassing parts of northern India and eastern Pakistan. Pick one labelled image and give it the whole five minutes. Keep the page open, but save its description for minute four.')
change(8,1,1,'If the pattern stays dense throughout, follow that density. Does one area feel more packed than another? Can a change in colour create breathing space even where there is little bare ground? A quiet area might be something your eye discovers rather than an obvious empty patch.')
change(8,2,1,'Simple descriptions work well here. “Short diagonal marks” gives you a line to follow; a small zigzag sketch gives you another. Save any unfamiliar stitch names for the next minute. For now, enjoy finding words that make the surface clearer to you.')
change(8,4,0,'Finish with a question about the detail that stayed with you. Perhaps you want a reverse view or a closer look at the boundary you sketched. “What happens at this edge?” has somewhere to go. Put the question at the foot of your page like a small invitation to return.')

change(9,0,0,'UNESCO’s account of the Margilan Crafts Development Center describes practical and theoretical training, local knowledge transmission, and workshops across several crafts. Its safeguarding work includes atlas and adras making. Here, a textile story unfolds through people learning skills and passing them on, as well as through finished cloth.')
change(9,2,1,'A short process video may make you want to pause on one action: fingers arranging thread, a colour meeting cloth, a repeated movement finding its rhythm. Ask which piece or batch the film follows. The answer can keep the moment attached to the textile whose story you are reading.')
change(9,3,0,'You may admire the spacing of stitches or the way a border is resolved. Name the detail that held your attention. A small, precise compliment can make the work visible: this turn, this join, this patient sequence. It gives the person you are speaking to something particular to explain.')
change(9,3,1,'Other questions may still interest you: fibre composition, working conditions, environmental impact, or how an item wears over time. Ask them in their own right. Handmade describes a kind of work; those further answers can bring more dimensions to the story when the producer has information to share.')
change(9,4,0,'When you retell a textile story, keep the names and roles the producer has shared with permission. A weaver, a garment maker, and a workshop can each have a place. If someone remains unidentified, leave room beside their stage. The work is still there to be noticed.')
change(9,4,1,'A memorable account can explain one difficult step, show how another depends on it, and introduce the people it can name. You leave able to picture work happening, with a better question for the next textile. The word handmade becomes the beginning of a conversation you would like to continue.')

# Persist the polish in the generator too, so regenerating cannot restore the older copy.
generator=base/'och-write-posts.py'
text=generator.read_text()
original=json.loads((base/'posts.json').read_text())
for old,new in zip(original,posts):
    for old_section,new_section in zip(old['sections'],new['sections']):
        text=text.replace(old_section['heading'],new_section['heading'])
        for old_para,new_para in zip(old_section['paragraphs'],new_section['paragraphs']):
            if old_para!=new_para:
                # Generator stores paragraphs as Python single-quoted literals.
                old_literal=repr(old_para)
                new_literal=repr(new_para)
                # Most originals use single quotes; quote presence is handled with literal replacement.
                if old_literal in text:
                    text=text.replace(old_literal,new_literal)
                else:
                    text=text.replace("'"+old_para+"'",new_literal)
generator.write_text(text)
(base/'posts.json').write_text(json.dumps(posts,ensure_ascii=False,indent=2)+'\n')
print('Polished ten records.')
