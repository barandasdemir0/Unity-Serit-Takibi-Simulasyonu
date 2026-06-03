using UnityEngine;

/// <summary>
/// Bresenham Çizgi Çizim Algoritmasını uygulayan saf matematik sınıfı.
/// BAĞLANTI: GraphRenderer.cs tarafından, iki piksel noktası arasında mükemmel bir doğru çizmek için kullanılır.
/// Performanslı bir şekilde ekrana çizgi çizen bilgisayar grafikleri temel algoritmasıdır.
/// </summary>
public static class BresenhamLineDrawer
{
    /// <summary>
    /// Renk dizisi (pixels) üzerinde (x0,y0) noktasından (x1,y1) noktasına çizgi çeker.
    /// </summary>
    /// <param name="pixels">Grafiğin çizileceği 1 boyutlu piksel/renk dizisi (Texture2D verisi)</param>
    /// <param name="width">Grafik ekranının genişliği</param>
    /// <param name="height">Grafik ekranının yüksekliği</param>
    /// <param name="x0">Başlangıç noktası X koordinatı</param>
    /// <param name="y0">Başlangıç noktası Y koordinatı</param>
    /// <param name="x1">Bitiş noktası X koordinatı</param>
    /// <param name="y1">Bitiş noktası Y koordinatı</param>
    /// <param name="color">Çizginin rengi</param>
    public static void DrawLine(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color)
    {
        // Delta X ve Delta Y: Başlangıç ve bitiş arasındaki mutlak mesafeler
        int dx = Mathf.Abs(x1 - x0);
        int dy = -Mathf.Abs(y1 - y0);
        
        // Step X ve Step Y: Çizginin gidiş yönüne göre (1 veya -1) piksellerin artış yönü
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        
        // Error (Hata payı): Çizginin ideal doğrudan ne kadar saptığını takip eder
        int err = dx + dy;

        while (true)
        {
            // Eğer koordinatlar grafik sınırları (width, height) içindeyse o pikseli boya
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                // 2 Boyutlu koordinatı, 1 Boyutlu piksel dizisindeki indeks'e çevir: (Y * Genişlik) + X
                pixels[y0 * width + x0] = color;
            }

            // Eğer bitiş noktasına ulaştıysak döngüyü (çizim işlemini) bitir
            if (x0 == x1 && y0 == y1) break;
            
            // Hata miktarının 2 katını al
            int e2 = 2 * err;
            
            // X ekseninde ilerlememiz gerekiyorsa X koordinatını kaydır
            if (e2 >= dy) { err += dy; x0 += sx; }
            
            // Y ekseninde ilerlememiz gerekiyorsa Y koordinatını kaydır
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
}
