using System;

public static class Util {
    public static float Integrate(Func<float, float> f, float a, float b, float n) {
        float h = (b - a) / n;
        float sum = 0.5f * (f(a) + f(b));

        for (int i = 0; i < n; i++) {
            float x = a + i * h;
            sum += f(x);
        }

        return h * sum;
    }
}