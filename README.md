# Twitch Etkileşimli Balon Oyunu

Unity 6 ile hazırlanmış, Twitch chat üzerinden varsayılan olarak `!inflate` komutuyla çalışan yayın içi mini oyun prototipi.

## Hızlı başlangıç

1. Unity Hub üzerinden `balon` klasörünü açın.
2. Unity derlemeyi bitirdiğinde `Tools > Balloon Game > Build Editable Prototype Scene` seçeneğini çalıştırın.
3. `Assets/Scenes/BalloonGame.unity` sahnesini açıp Play'e basın.
4. Yerel testte:
   - `Space`: bir test oyuncusu ekler.
   - `A`: beş test oyuncusu ekler.
   - `P`: balonu sonraki şişirmede patlatır.

Sahne ilk kurulum sırasında otomatik üretilmiştir. Aynı menü komutu sahneyi varsayılan düzenle yeniden oluşturur.

## Elle düzenleme

- Genel oyun ve Twitch ayarları: `Assets/BalloonGame/Config/BalloonGameSettings.asset`
- Tüm görsel parçalar: `Balloon Game Canvas > Scene Content`
- Balon: `Balloon Stage > Balloon Assembly > Balloon Visual Root`
- Pompa: `Hand Pump (fully editable)`
- Patlama parçaları: `Explosion Shards (edit children)`
- Sistem bağlantıları: `Game Systems`

Her görünür öğe ayrı bir GameObject'tir. Renk, boyut, konum, yazı, animasyon süresi, kuyruk limiti ve patlama aralığı Inspector üzerinden değiştirilebilir.

## Twitch bağlantısı

Uygulama ilk olarak `TwitchLogin` sahnesini açar. Daha önce kaydedilmiş Twitch oturumu `/validate` ile geçerliyse kare oyun ekranına otomatik geçilir. Oturum yoksa son kullanıcı yalnızca **Twitch ile giriş yap** düğmesini görür.

Twitch Developer Console'da uygulamayı **Public Client** olarak oluşturun. Developer Console'dan aldığınız `Client ID` değerini `Assets/BalloonGame/Config/BalloonGameSettings.asset` içindeki `Twitch Client Id` alanına geliştirme sırasında bir kez yazın. Son kullanıcı bu değeri girmez veya görmez. Device Code Grant akışı client secret gerektirmez.

Kullanıcı giriş yaptığında `chat:read` ve `chat:edit` yetkili access/refresh tokenları alınır. Bu tokenlar cihaza özel AES anahtarıyla şifrelenerek yerel uygulama klasörüne kaydedilir ve chat okuma/yazma işlemlerinde kullanılır.

İsterseniz eski ortam değişkeni yöntemi de kullanılabilir:

- `TWITCH_CHANNEL`: dinlenecek kanal
- `TWITCH_BOT_USERNAME`: tokenın ait olduğu Twitch hesabı
- `TWITCH_OAUTH_TOKEN`: `chat:read` yetkili User Access Token

Ardından `BalloonGameSettings` içinde `Connect To Twitch On Start` seçeneğini açın. İsterseniz kanal ve kullanıcı adını asset üzerinde tutabilirsiniz; tokenı sürüm kontrolündeki asset içine yazmayın.

Bu prototip, aktif olarak desteklenen SSL IRC bağlantısını kullanır. Twitch yeni ve daha kapsamlı chatbot uygulamalarında EventSub + Twitch API yaklaşımını önerdiği için üretim sürümünde entegrasyon katmanı EventSub ile değiştirilebilir; oyun mantığı bundan bağımsız tutulmuştur.

## Oyun içi ayarlar

Fare oyun penceresinin üzerine geldiğinde sağ üstte `SETTINGS` düğmesi görünür. Ayarlar kullanıcıya özel olarak `Application.persistentDataPath` altında JSON dosyasında tutulur.

Panelden komut, patlama chat mesajı, arka plan rengi/şeffaflığı, balon rengi, oyuncu adları, katılım kuralı, cooldown, patlama aralığı, kuyruk kapasitesi ve animasyon süresi değiştirilebilir. Patlama mesajında `{player}` görünen Twitch adını, `{username}` ise kullanıcı giriş adını temsil eder. Şeffaf arka plan seçeneği render edilen arka planın alfa değerini sıfırlar; OBS tarafında kullanılan yakalama yönteminin alfa desteği açık olmalıdır.

## Language file

Oyunda kullanıcıya gösterilen bütün sabit metinler `Assets/BalloonGame/Config/BalloonLanguageEnglish.asset` dosyasında tutulur. Yeni bir kullanıcı arayüzü metni eklenirken doğrudan script veya sahne içine yazılmamalı, önce `BalloonLanguage` assetine alan olarak eklenmelidir. Yeni bir çeviri için bu asset çoğaltılıp `BalloonGameSettings > Language` alanına atanabilir.
