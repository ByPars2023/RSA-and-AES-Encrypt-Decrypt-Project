using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var uploadDirectory = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
Directory.CreateDirectory(uploadDirectory);

app.MapGet("/", () => Results.Content(HtmlPage, "text/html"));

app.MapPost("/api/upload", async Task<IResult> (IFormFile? pdfFile) =>
{
    if (pdfFile is null || pdfFile.Length == 0)
    {
        return Results.BadRequest(new { message = "Lütfen bir PDF dosyası seçin." });
    }

    if (!pdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Sadece PDF dosyaları yüklenebilir." });
    }

    var safeFileName = SanitizeFileName(pdfFile.FileName);
    var targetPath = Path.Combine(uploadDirectory, safeFileName);

    await using var stream = File.Create(targetPath);
    await pdfFile.CopyToAsync(stream);

    return Results.Ok(new
    {
        message = "PDF başarıyla yüklendi.",
        fileName = safeFileName,
        downloadUrl = $"/uploads/{safeFileName}"
    });
});

app.MapFallback(() => Results.Content(HtmlPage, "text/html"));

app.Run();

static string SanitizeFileName(string fileName)
{
    var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
    var invalidRegex = new Regex($"[{invalidChars}]+", RegexOptions.Compiled);
    var cleaned = invalidRegex.Replace(fileName, "_");
    return string.IsNullOrWhiteSpace(cleaned) ? $"pdf_{Guid.NewGuid():N}.pdf" : cleaned;
}

const string HtmlPage = """
<!doctype html>
<html lang=\"tr\">
<head>
  <meta charset=\"UTF-8\" />
  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />
  <title>PDF Yükleme ve Şifreleme</title>
  <style>
    :root {
      color-scheme: light dark;
      --bg: #0f172a;
      --panel: #1e293b;
      --accent: #6366f1;
      --accent-2: #22c55e;
      --text: #e2e8f0;
      --muted: #94a3b8;
      font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
    }

    * { box-sizing: border-box; }

    body {
      margin: 0;
      min-height: 100vh;
      display: grid;
      place-items: center;
      background: radial-gradient(circle at 20% 20%, #1e293b 0, #0f172a 35%, #0b1224 100%);
      color: var(--text);
    }

    .card {
      width: min(720px, 92vw);
      background: linear-gradient(145deg, rgba(99, 102, 241, 0.12), rgba(34, 197, 94, 0.12));
      border: 1px solid rgba(148, 163, 184, 0.25);
      border-radius: 20px;
      padding: 28px;
      backdrop-filter: blur(12px);
      box-shadow: 0 20px 80px rgba(0, 0, 0, 0.35);
    }

    h1 {
      margin: 0 0 10px;
      font-size: 28px;
      letter-spacing: 0.5px;
    }

    p.subtitle {
      margin: 0 0 22px;
      color: var(--muted);
      line-height: 1.5;
    }

    .upload-area {
      border: 2px dashed rgba(148, 163, 184, 0.4);
      border-radius: 14px;
      padding: 24px;
      text-align: center;
      transition: border-color 0.2s ease, background-color 0.2s ease;
      background: rgba(15, 23, 42, 0.6);
    }

    .upload-area.dragover {
      border-color: var(--accent);
      background: rgba(99, 102, 241, 0.08);
    }

    .upload-area input { display: none; }

    .upload-label {
      display: inline-flex;
      align-items: center;
      gap: 10px;
      padding: 12px 18px;
      border-radius: 12px;
      background: linear-gradient(135deg, var(--accent), var(--accent-2));
      color: white;
      cursor: pointer;
      font-weight: 600;
      box-shadow: 0 10px 30px rgba(99, 102, 241, 0.4);
      border: none;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .inputs { display: grid; gap: 14px; margin-top: 20px; }

    label { font-weight: 600; letter-spacing: 0.3px; }

    input[type="text"] {
      width: 100%;
      padding: 12px;
      border-radius: 12px;
      border: 1px solid rgba(148, 163, 184, 0.3);
      background: rgba(15, 23, 42, 0.7);
      color: var(--text);
    }

    .actions {
      display: flex;
      gap: 12px;
      margin-top: 22px;
      flex-wrap: wrap;
    }

    button.action {
      flex: 1;
      min-width: 200px;
      padding: 12px 16px;
      border-radius: 12px;
      border: 1px solid rgba(148, 163, 184, 0.3);
      background: rgba(99, 102, 241, 0.15);
      color: var(--text);
      cursor: pointer;
      font-weight: 700;
      letter-spacing: 0.4px;
      transition: transform 0.15s ease, box-shadow 0.15s ease;
    }

    button.action:hover { transform: translateY(-1px); box-shadow: 0 10px 30px rgba(0,0,0,0.3); }

    .status {
      margin-top: 18px;
      padding: 14px;
      border-radius: 12px;
      background: rgba(34, 197, 94, 0.12);
      border: 1px solid rgba(34, 197, 94, 0.25);
      color: #bbf7d0;
      display: none;
    }

    .status.error {
      background: rgba(248, 113, 113, 0.12);
      border-color: rgba(248, 113, 113, 0.35);
      color: #fecdd3;
    }

    footer {
      margin-top: 16px;
      color: var(--muted);
      font-size: 13px;
      text-align: center;
    }
  </style>
</head>
<body>
  <main class=\"card\">
    <h1>PDF Yükle ve Şifrele</h1>
    <p class=\"subtitle\">PDF dosyanızı yükleyin, ardından RSA/AES anahtarlarını girerek şifreleme veya çözme işlemlerine hazırlanın.</p>

    <div id=\"upload-area\" class=\"upload-area\">
      <input id=\"pdfInput\" type=\"file\" accept=\"application/pdf\" />
      <label class=\"upload-label\" for=\"pdfInput\">PDF Seç / Sürükle</label>
      <p class=\"subtitle\" style=\"margin-top: 12px;\">Sürükle-bırak veya butonu kullanarak dosya seçin.</p>
    </div>

    <div class=\"inputs\">
      <div>
        <label for=\"rsaKey\">RSA Açık Anahtarı</label>
        <input id=\"rsaKey\" type=\"text\" placeholder=\"Örn: MIIBIjANBgkqhkiG9...\" />
      </div>
      <div>
        <label for=\"aesKey\">AES Anahtarı</label>
        <input id=\"aesKey\" type=\"text\" placeholder=\"32 karakterlik anahtar girin\" />
      </div>
    </div>

    <div class=\"actions\">
      <button class=\"action\" id=\"encryptBtn\">Şifrelemeye Hazırla</button>
      <button class=\"action\" id=\"decryptBtn\">Deşifre Etmeye Hazırla</button>
    </div>

    <div id=\"status\" class=\"status\"></div>

    <footer>Bu arayüz yalnızca yükleme ve hazırlık adımlarını gösterir. Şifreleme/deşifre işlemleri sonraki adımda eklenecektir.</footer>
  </main>

  <script>
    const uploadArea = document.getElementById('upload-area');
    const pdfInput = document.getElementById('pdfInput');
    const statusEl = document.getElementById('status');
    const encryptBtn = document.getElementById('encryptBtn');
    const decryptBtn = document.getElementById('decryptBtn');

    const setStatus = (message, isError = false) => {
      statusEl.textContent = message;
      statusEl.classList.toggle('error', isError);
      statusEl.style.display = 'block';
    };

    const resetStatus = () => {
      statusEl.style.display = 'none';
      statusEl.textContent = '';
      statusEl.classList.remove('error');
    };

    const uploadPdf = async (file) => {
      const formData = new FormData();
      formData.append('pdfFile', file);

      const response = await fetch('/api/upload', {
        method: 'POST',
        body: formData
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Yükleme başarısız.');
      }

      return response.json();
    };

    pdfInput.addEventListener('change', async (event) => {
      const file = event.target.files?.[0];
      if (!file) return;

      resetStatus();
      setStatus('Yükleniyor...');

      try {
        const result = await uploadPdf(file);
        setStatus(`${result.message} Dosya: ${result.fileName}`);
      } catch (err) {
        setStatus(err.message, true);
      }
    });

    ['dragenter', 'dragover'].forEach(evt => uploadArea.addEventListener(evt, e => {
      e.preventDefault();
      e.stopPropagation();
      uploadArea.classList.add('dragover');
    }));

    ['dragleave', 'drop'].forEach(evt => uploadArea.addEventListener(evt, e => {
      e.preventDefault();
      e.stopPropagation();
      uploadArea.classList.remove('dragover');
    }));

    uploadArea.addEventListener('drop', async (e) => {
      const file = e.dataTransfer.files?.[0];
      if (!file) return;
      if (file.type !== 'application/pdf') {
        setStatus('Lütfen sadece PDF dosyası bırakın.', true);
        return;
      }

      resetStatus();
      setStatus('Yükleniyor...');

      try {
        const result = await uploadPdf(file);
        setStatus(`${result.message} Dosya: ${result.fileName}`);
      } catch (err) {
        setStatus(err.message, true);
      }
    });

    encryptBtn.addEventListener('click', () => {
      setStatus('Şifreleme adımları yakında eklenecek. PDF ve anahtarlar hazır.', false);
    });

    decryptBtn.addEventListener('click', () => {
      setStatus('Deşifre adımları yakında eklenecek. PDF ve anahtarlar hazır.', false);
    });
  </script>
</body>
</html>
""";
