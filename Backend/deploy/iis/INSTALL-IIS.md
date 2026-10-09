# NetCha API — IIS 10 kurulumu

Bu paket .NET 10 için framework-dependent olarak yayımlanmıştır. Sunucuda
ASP.NET Core Hosting Bundle 10 kurulu olmalıdır.

## 1. Dosyaları yerleştirin

ZIP içeriğini örneğin aşağıdaki klasöre çıkarın:

```text
C:\inetpub\wwwroot\netcha-api
```

Uygulama havuzu kimliğine bu klasör için `Read & execute` izni verin.

## 2. IIS sitesini oluşturun

- Application Pool: `NetChaApi`
- .NET CLR version: `No Managed Code`
- Managed pipeline mode: `Integrated`
- Site physical path: `C:\inetpub\wwwroot\netcha-api`
- HTTPS host name: `netcha.netisler.com`
- HTTPS sertifikası: bu alan adı için geçerli ve güvenilir bir sertifika

ASP.NET Core Hosting Bundle kurulumundan sonra IIS'i yeniden başlatın.

## 3. Production değişkenlerini tanımlayın

Değerleri makine, uygulama havuzu veya IIS `aspNetCore/environmentVariables`
seviyesinde tanımlayın. Gerçek anahtarları ZIP dosyasına ya da kaynak koduna
yazmayın.

Zorunlu değişkenler:

```text
ASPNETCORE_ENVIRONMENT=Production
Captcha__HmacKey=<en az 32 byte rastgele gizli değer>
Admin__ApiKey=<ilk admin hesabı için tek kullanımlık başlangıç parolası>
Admin__SessionHours=8
Cors__AllowedOrigins__0=https://tayfuns.github.io
```

Önerilen production değişkenleri:

```text
Persistence__ConnectionString=<PostgreSQL/Supabase bağlantısı>
Persistence__AutoMigrate=false
Persistence__FallbackToMemory=true
Redis__ConnectionString=<Redis bağlantısı>
Redis__FallbackToMemory=true
```

İlk kontrollü veritabanı kurulumunda `Persistence__AutoMigrate=true` kullanın.
Bu işlem `netcha_private.admin_users` ve `netcha_private.admin_sessions`
tablolarını oluşturup `Admin__ApiKey` değerini `admin` kullanıcısının başlangıç
parolası olarak güvenli biçimde hashler. İlk girişten sonra panelde **Hesap
güvenliği** sayfasından parolayı değiştirin, `Admin__ApiKey` satırını tamamen
kaldırın ve `Persistence__AutoMigrate=false` yapın.

PowerShell ile güvenli rastgele değer üretme örneği:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

## 4. Yayını doğrulayın

Uygulama havuzunu geri dönüştürdükten sonra sırayla kontrol edin:

```text
GET https://netcha.netisler.com/health
GET https://netcha.netisler.com/openapi/v1.json
GET https://netcha.netisler.com/api/v1/captcha/challenge
POST https://netcha.netisler.com/api/v1/admin/auth/login
    { "username": "admin", "password": "<başlangıç parolası>" }
```

Beklenen sonuçlar:

- `/health`: HTTP 200
- challenge: `algorithm`, `challenge`, `salt`, `signature` alanları
- admin girişi doğru kullanıcı/parolayla oturum tokenı döndürür
- GitHub Pages demosunda CAPTCHA çözümü ve form gönderimi başarılı
- Aynı CAPTCHA payload'u ikinci kez gönderildiğinde replay reddedilir

## 5. Sorun giderme

- HTTP 500.30/500.31: Hosting Bundle sürümünü ve Event Viewer kayıtlarını kontrol edin.
- HTTP 502.5: Application Pool'u yeniden başlatın; `dotnet --list-runtimes` çıktısında .NET 10 bulunduğunu doğrulayın.
- CORS hatası: izin verilen origin değerinin tam olarak `https://tayfuns.github.io` olduğundan emin olun.
- Geçici tanılama gerekiyorsa `web.config` içindeki `stdoutLogEnabled` değerini kısa süreli `true` yapın ve `logs` klasörüne yazma izni verin. Sorun çözülünce tekrar kapatın.

