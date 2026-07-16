# Twitch Etkileşimli Balon Oyunu

Unity 6 ile hazırlanmış, Twitch chat üzerinden `!sisir` komutuyla çalışan yayın içi mini oyun prototipi.

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

Twitch IRC bağlantısı için `chat:read` kapsamına sahip bir User Access Token gerekir. Önerilen kurulum, gizli bilgileri asset içine yazmak yerine uygulamayı şu ortam değişkenleriyle başlatmaktır:

- `TWITCH_CHANNEL`: dinlenecek kanal
- `TWITCH_BOT_USERNAME`: tokenın ait olduğu Twitch hesabı
- `TWITCH_OAUTH_TOKEN`: `chat:read` yetkili User Access Token

Ardından `BalloonGameSettings` içinde `Connect To Twitch On Start` seçeneğini açın. İsterseniz kanal ve kullanıcı adını asset üzerinde tutabilirsiniz; tokenı sürüm kontrolündeki asset içine yazmayın.

Bu prototip, aktif olarak desteklenen SSL IRC bağlantısını kullanır. Twitch yeni ve daha kapsamlı chatbot uygulamalarında EventSub + Twitch API yaklaşımını önerdiği için üretim sürümünde entegrasyon katmanı EventSub ile değiştirilebilir; oyun mantığı bundan bağımsız tutulmuştur.
