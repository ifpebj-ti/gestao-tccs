'use client'; // necessário se estiver usando App Router

import Image from "next/image";
import { useEffect, useState } from "react";

type WeatherForecast = {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
};

export default function Home() {
  const [forecasts, setForecasts] = useState<WeatherForecast[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchForecasts = async () => {
      try {
        const response = await fetch("http://localhost:8080/api/weatherForecast");
        if (!response.ok) {
          throw new Error("Erro ao buscar dados da API");
        }
        const data = await response.json();
        setForecasts(data);
      } catch (error) {
        console.error("Erro ao buscar dados da previsão do tempo:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchForecasts();
  }, []);

  return (
    <div className="grid grid-rows-[20px_1fr_20px] items-center justify-items-center min-h-screen p-8 pb-20 gap-16 sm:p-20 font-[family-name:var(--font-geist-sans)]">
      <main className="flex flex-col gap-[32px] row-start-2 items-center sm:items-start">
        <Image
          className="dark:invert"
          src="/next.svg"
          alt="Next.js logo"
          width={180}
          height={38}
          priority
        />

        {loading ? (
          <p>Carregando previsão do tempo...</p>
        ) : (
          <ul className="text-sm">
            {forecasts.map((forecast, index) => (
              <li key={index} className="mb-2">
                {forecast.date}: {forecast.temperatureC}°C - {forecast.summary}
              </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  );
}
