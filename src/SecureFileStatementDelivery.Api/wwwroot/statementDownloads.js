(() => {
  const tokenInput = document.getElementById('token');
  const jwtInput = document.getElementById('jwt');
  const rememberCheckbox = document.getElementById('remember');
  const downloadBtn = document.getElementById('downloadBtn');
  const clearBtn = document.getElementById('clearBtn');
  const output = document.getElementById('output');

  function setOutput(message) {
    output.textContent = message;
  }

  function getQueryToken() {
    const url = new URL(window.location.href);
    const token = url.searchParams.get('token');
    return token ? token.trim() : '';
  }

  function loadRememberedJwt() {
    try {
      const saved = localStorage.getItem('sfsd.jwt');
      if (saved) {
        jwtInput.value = saved;
        rememberCheckbox.checked = true;
      }
    } catch {
      
    }
  }

  function persistJwtIfRequested() {
    try {
      if (rememberCheckbox.checked) {
        localStorage.setItem('sfsd.jwt', jwtInput.value.trim());
      } else {
        localStorage.removeItem('sfsd.jwt');
      }
    } catch {
      
    }
  }

  function extractFileName(contentDisposition) {
    if (!contentDisposition) return null;

     const match = /filename\*?=(?:UTF-8''|\")?([^;\"\n]+)/i.exec(contentDisposition);
    if (!match) return null;

    const raw = match[1].trim().replace(/\"$/, '');
    try {
      return decodeURIComponent(raw);
    } catch {
      return raw;
    }
  }

  async function downloadPdf() {
    const token = tokenInput.value.trim();
    const jwt = jwtInput.value.trim();

    if (!token) {
      setOutput('Missing token.');
      return;
    }

    if (!jwt) {
      setOutput('Missing JWT.');
      return;
    }

    persistJwtIfRequested();

    const url = `${window.location.origin}/downloads/${encodeURIComponent(token)}`;
    setOutput(`Requesting ${url} ...`);

    let response;
    try {
      response = await fetch(url, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${jwt}`
        }
      });
    } catch (err) {
      setOutput(`Network error: ${err}`);
      return;
    }

    if (!response.ok) {
      const text = await response.text().catch(() => '');
      setOutput(`HTTP ${response.status} ${response.statusText}\n${text}`);
      return;
    }

    const blob = await response.blob();
    const cd = response.headers.get('content-disposition');
    const fileName = extractFileName(cd) ?? 'statement.pdf';

    const objectUrl = URL.createObjectURL(blob);
    try {
      const a = document.createElement('a');
      a.href = objectUrl;
      a.download = fileName;
      a.rel = 'noopener';
      document.body.appendChild(a);
      a.click();
      a.remove();

      setOutput(`Downloaded ${fileName} (${blob.size} bytes).`);
    } finally {
      URL.revokeObjectURL(objectUrl);
    }
  }

  function clearAll() {
    tokenInput.value = '';
    jwtInput.value = '';
    rememberCheckbox.checked = false;
    try {
      localStorage.removeItem('sfsd.jwt');
    } catch {
      
    }
    setOutput('Cleared.');
  }

 
  const tokenFromQuery = getQueryToken();
  if (tokenFromQuery) {
    tokenInput.value = tokenFromQuery;
  }

  loadRememberedJwt();
  setOutput('Ready.');

  downloadBtn.addEventListener('click', () => {
    downloadPdf();
  });

  clearBtn.addEventListener('click', () => {
    clearAll();
  });
})();
