# NetCha Frontend

React 19 + TypeScript + Vite workspace. İki bağımsız uygulama içerir:

- `apps/captcha-demo`: ALTCHA web component ile gerçek challenge/verify akışı
- `apps/admin-panel`: sağlık, istatistik, audit ve salt-okunur etkin ayarlar
- `packages/shared`: API istemcisi ve ortak görsel temel

Node.js 22 veya üzeri gerekir. Geliştirme sırasında doğrulanan sürüm Node `24.20.0`, npm `11.19.0`.

## Yerel geliştirme

```bash
cd Frontend
npm install
cp apps/captcha-demo/.env.example apps/captcha-demo/.env.local
cp apps/admin-panel/.env.example apps/admin-panel/.env.local
npm run dev:demo
```

Admin paneli ayrı terminalde başlatılır:

```bash
npm run dev:admin
```

Varsayılan API adresi `http://localhost:5080`'dır. Uygulamalara erişim adresini Vite'ın terminal çıktısı gösterir. Backend CORS yapılandırmasının kullanılan frontend origin'ine izin vermesi gerekir.

Admin anahtarı bundle içine veya `.env` dosyasına konmaz. Giriş ekranında alınır, yalnızca `sessionStorage` içinde tutulur ve her admin isteğinde `X-Admin-Key` başlığıyla gönderilir.

## API sözleşmesi

Demo uygulaması:

- `GET /api/v1/captcha/challenge`
- `POST /api/v1/captcha/verify` — `{ "payload": "<ALTCHA base64 payload>" }`

Admin paneli:

- `GET /health` (text/plain)
- `GET /api/v1/admin/stats`
- `GET /api/v1/admin/audit?limit=100`
- `GET /api/v1/admin/settings`

Tüm yollar `.env.example` içindeki değişkenlerle değiştirilebilir.

## Kalite kontrolleri

```bash
npm run typecheck
npm test
npm run build
```

## GitHub Pages

Varsayılan base path'ler demo için `/netcha/`, admin için `/netcha/admin/` olarak ayarlıdır. Repository adı veya yayın yolu farklıysa iki uygulamanın `VITE_BASE_PATH` değerini güncelleyin.

```bash
npm run build:pages
```

Bu komut GitHub Pages'e yüklenebilecek birleşik çıktıyı `dist-pages/` altında oluşturur; admin paneli `dist-pages/admin/` içine yerleştirilir. Üretim API adresini build sırasında verin:

```bash
VITE_API_URL=https://netcha.netisler.com npm run build:pages
```

Hiçbir secret frontend build değişkenine konmamalıdır; `VITE_` değişkenleri ziyaretçilere açıktır.
