## WorkPlanCalender (ASP.NET MVC 5)

Kurumsal verilerden arındırılmış demo sürüm. `DemoMode` açıkken uygulama veritabanına bağlanmadan örnek kullanıcı ve görevlerle çalışır.

### Özellikler
- FullCalendar ile görev planlama (sürükle-bırak, resize)
- `DemoMode` ile mock kullanıcı ve görev verileri
- Yapılandırılabilir bilet bağlantısı (`TicketBaseUrl`)
- (Opsiyonel) GitHub Actions CI: Her push’ta otomatik derleme

### Hızlı Başlangıç
1) Visual Studio ile `CalenderDemo2.sln` dosyasını açın.
2) Başlangıç projesi olarak `CalenderDemo2` seçin ve F5 ile çalıştırın.

### Demo Modu
`CalenderDemo2/Web.config` içinde:
```xml
<appSettings>
  <add key="DemoMode" value="true" />
  <add key="TicketBaseUrl" value="https://example.com/qbt/Ticket/Browse/QBT-" />
</appSettings>
```
- `DemoMode=true`: Kullanıcılar ve görevler bellek içinde üretilir; DB yok.
- `DemoMode=false`: Entity Framework ile gerçek DB’ye bağlanır (kendi bağlantılarınızı tanımlayın).

### CI (Continuous Integration) nedir?
- Repoya her push/pull request’te sunucuda otomatik derleme çalıştırır.
- Kırık build’leri erken yakalarsınız.
- Örnek workflow dosyası: `.github/workflows/ci.yml` (Windows runner’da NuGet restore + MSBuild ile derleme).

### GitHub’a Yükleme (kısaca)
```bash
# proje kökünde
git init
git branch -M main
git add .
git commit -m "Initial import (sanitized + DemoMode)"
# kendi repo URL’nizi kullanın
git remote add origin https://github.com/<kullanici-adi>/<repo-adi>.git
git push -u origin main
```

### Notlar
- `.gitignore` takip dışı: `bin/`, `obj/`, `*.user`, publish profilleri vb.
- Demo verileri uygulama yeniden başlatıldığında sıfırlanır (in-memory).
