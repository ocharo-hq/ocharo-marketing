(() => {
  const form=document.querySelector('[data-filter-form]');
  if(form){
    const cards=[...document.querySelectorAll('[data-filter-grid] [data-item]')];
    const parameters=new URLSearchParams(location.search);
    const segments=location.pathname.split('/').filter(Boolean);
    for(const axis of ['places','techniques','motifs','occasions']){
      const index=segments.indexOf(axis);if(index>=0&&segments[index+1])parameters.set(axis,decodeURIComponent(segments[index+1]));
    }
    for(const field of form.querySelectorAll('input[name],select[name]'))field.value=parameters.get(field.name)||'';
    const update=(history=false)=>{
      const values=Object.fromEntries(new FormData(form));let count=0;
      for(const card of cards){const matches=Object.entries(values).every(([key,value])=>!value||key==='locale'||(key==='q'?(card.dataset.search||'').toLocaleLowerCase().includes(String(value).toLocaleLowerCase()):(card.dataset[key]||'').split('|').includes(String(value))));card.hidden=!matches;if(matches)count++;}
      const status=document.querySelector('[data-count]');if(status)status.textContent=`${count} of ${cards.length} ${location.pathname.includes('/catalogue')?'pieces':'results'}`;
      const empty=document.querySelector('[data-empty]');if(empty)empty.hidden=count>0;
      if(history){const query=new URLSearchParams(Object.entries(values).filter(([,value])=>value));window.history.replaceState(null,'',location.pathname+(query.size?'?'+query:''));}
    };
    form.addEventListener('submit',event=>{event.preventDefault();update(true);});form.addEventListener('input',()=>update(true));form.addEventListener('change',()=>update(true));form.addEventListener('reset',()=>setTimeout(()=>update(true),0));update();
  }
  for(const button of document.querySelectorAll('[data-copy]'))button.addEventListener('click',async()=>{
    const status=button.parentElement.querySelector('.copy-status');
    try{await navigator.clipboard.writeText(button.dataset.copy);status.textContent='Enquiry text copied. Paste it into your conversation with Ocharo.';}catch{status.textContent=button.dataset.copy;}
  });
})();

for (const form of document.querySelectorAll('[data-inquiry]')) {
  let submissionId=crypto.randomUUID();
  form.addEventListener('submit',async event=>{
    event.preventDefault(); if(!form.reportValidity())return;
    const button=form.querySelector('[type=submit]'), status=form.querySelector('[data-inquiry-status]');button.disabled=true;status.textContent='Saving your enquiry…';
    const values=new FormData(form), body={submissionId,name:values.get('name'),contactMethod:values.get('contactMethod'),contact:values.get('contact'),message:values.get('message'),sourcePath:location.pathname,productId:form.dataset.product,consent:values.get('consent')==='on',website:values.get('website')};
    try{const response=await fetch('/api/public/inquiries',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});const result=await response.json();if(!response.ok)throw new Error(result.detail??'The enquiry could not be saved.');status.textContent='Your enquiry is saved. Reference '+result.reference+'. No order or payment was placed.';form.reset();submissionId=crypto.randomUUID();}
    catch(error){status.textContent=error.message??'Connection unavailable. Your message stays here; try again.';}
    finally{button.disabled=false;}
  });
}

for (const button of document.querySelectorAll('[data-share-page]')) {
  button.addEventListener('click', async () => {
    const status = button.parentElement.querySelector('.copy-status');
    const url = location.origin + location.pathname + location.search;
    try { await navigator.clipboard.writeText(url); status.textContent = 'Page link copied.'; }
    catch { status.textContent = url; }
  });
}


(() => {
  const form = document.querySelector('[data-catalogue-discovery]');
  if (!form) return;
  const grid = document.querySelector('[data-catalogue-grid]');
  const cards = [...grid.querySelectorAll('[data-catalogue-item]')];
  const fields = [...form.querySelectorAll('[name]')];
  const locale = form.elements.namedItem('locale').value;
  const collator = new Intl.Collator(locale, { sensitivity: 'base' });
  const normalize = value => value.normalize('NFKC').toLowerCase().replace(/[’‘ʻʼ]/gu, "'");
  const readUrl = () => {
    const query = new URLSearchParams(location.search);
    for (const field of fields) {
      if (field.name === 'locale') continue;
      const value = (query.get(field.name) || '').trim().slice(0, 160);
      if (field.tagName === 'SELECT' && value && ![...field.options].some(option => option.value === value)) {
        if (field.name === 'sort') { field.value = ''; continue; }
        field.add(new Option(value, value));
      }
      field.value = value;
    }
  };
  const update = (writeHistory = false) => {
    const values = Object.fromEntries(new FormData(form));
    const terms = normalize(String(values.q || '').trim()).split(/\s+/u).filter(Boolean);
    let count = 0;
    for (const card of cards) {
      const data = card.dataset;
      const matches = terms.every(term => normalize(data.search || '').includes(term))
        && (!values.category || data.category === values.category)
        && (!values.fabric || data.fabric === values.fabric)
        && (!values.size || JSON.parse(data.sizes).includes(values.size));
      card.hidden = !matches;
      if (matches) count++;
    }
    const sort = values.sort;
    const ordered = cards.slice().sort((left, right) => {
      const original = Number(left.dataset.order) - Number(right.dataset.order);
      if (sort === 'name') return collator.compare(left.dataset.name, right.dataset.name) || original;
      if ((sort === 'price-asc' || sort === 'price-desc') && form.dataset.priceCurrency) {
        const leftKnown = left.dataset.price !== '', rightKnown = right.dataset.price !== '';
        if (leftKnown !== rightKnown) return leftKnown ? -1 : 1;
        if (leftKnown) {
          const difference = Number(left.dataset.price) - Number(right.dataset.price);
          return (sort === 'price-desc' ? -difference : difference) || original;
        }
      }
      return original;
    });
    grid.append(...ordered);
    document.querySelector('[data-catalogue-count]').textContent = `${count} of ${cards.length} pieces`;
    const empty = document.querySelector('[data-catalogue-empty]');
    if (empty) empty.hidden = count > 0;
    const query = new URLSearchParams(Object.entries(values).filter(([, value]) => value));
    if (writeHistory) window.history.replaceState(null, '', location.pathname + (query.size ? '?' + query : '') + location.hash);
    for (const link of document.querySelectorAll('.catalogue-languages a')) {
      const next = new URL(link.href); const targetLocale = next.searchParams.get('locale');
      next.search = query.toString(); next.searchParams.set('locale', targetLocale); next.hash = 'pieces';
      // Material and category labels belong to their locale; sizes and ordering remain meaningful.
      if (targetLocale !== locale) { next.searchParams.delete('category'); next.searchParams.delete('fabric'); }
      link.href = next.pathname + next.search + next.hash;
    }
  };
  form.addEventListener('submit', event => { event.preventDefault(); update(true); });
  form.addEventListener('input', event => { if (!event.isComposing) update(true); });
  form.addEventListener('compositionend', () => update(true));
  form.addEventListener('change', () => update(true));
  for (const link of document.querySelectorAll('[data-catalogue-reset]')) link.addEventListener('click', event => {
    if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button !== 0) return;
    event.preventDefault();
    for (const field of fields) if (field.name !== 'locale') field.value = '';
    update(true); form.elements.namedItem('q').focus();
  });
  window.addEventListener('popstate', () => { readUrl(); update(); });
  readUrl(); update();
})();
