[Zurück zur Gliederung](../../readme.md)

<br>

# Zusammenfassung und Ausblick

- [Zusammenfassung](#zusammenfassung)
- [Ausblick](#ausblick)

## Zusammenfassung

Der aktuelle Zustand der Bibliothek ermöglicht dem Nutzer folgende Dinge:

1. Die Simulation kann eine reale Produktion abbilden. Es werden Kunden, Kundenaufträge, Arbeitspläne, Produktionsaufträge, Arbeitsgänge auf Maschinen sowie benutzbare Werkzeuge simuliert
2. Die Simulation erzeugt Daten zur Auswertung (Rückmeldungen)
3. Die Simulation kann eine große Anzahl von Entitäten simulieren
4. Die Simulation ist vollständig konfigurier- und erweiterbar
5. Die Simulation erzeugt Simulationsdaten mit Hilfe eines kausalen Modells. Dieses Modell ist konfigurierbar.

Es ist möglich mit dem aktuellen Zustand der Bibliothek den Großteil der gestellten Anforderungen zu erfüllen. Es ist zu beachten, dass die Simulation dennoch eine vereinfachte Annahme der Realität ist und daher die Ergebnisse sorgfältig zu prüfen sind.

| Anforderung                                                                                          | Erfüllt (Ja/Nein/Teilweise) | Bemerkungen                                                                                                                                           |
| ---------------------------------------------------------------------------------------------------- | --------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| Die Anwendung soll in Python nutzbar sein.                                                           | Teilweise                   | Die genutzte Bibliothek hat im Besonderen Probleme mit abstrakten Typen und Interfaces. Es kann aber sein, dass diese Probleme in Zukunft beseitigt werden. |
| Es soll möglich sein, einen vorgegebenen Produktionsplan zu simulieren.                              | Ja                          |                                                                                                                                                       |
| Es soll möglich sein, einen Steuerungsalgorithmus vorzugeben.                                        | Ja                          |                                                                                                                                                       |
| Es soll möglich sein, einen Planungsalgorithmus vorzugeben.                                          | Ja                          |                                                                                                                                                       |
| Die Anwendung soll die Möglichkeit bieten, einen Produktionsprozess zu konfigurieren                | Ja                          |                                                                                                                                                       |
| Stammdaten der Produktion (Arbeitspläne, Maschinen, Werkzeuge)                                     | Ja                          |                                                                                                                                                       |
| Zufällige Ereignisse                                                                               | Ja                          |                                                                                                                                                       |
| Konkrete Produktionsaufträge                                                                       | Ja                          |                                                                                                                                                       |
| Die Anwendung soll die Ergebnisse der Simulation zur Verfügung stellen.                              | Ja                          |                                                                                                                                                       |
| Die Anwendung soll die Möglichkeit bieten, die Ergebnisse der Simulation zu visualisieren.           | Teilweise                   | Als CSV-Ausgabe                                                                                                                                       |
| Die Anwendung soll die Möglichkeit geben, mit einem Kausalen Modell Daten zu erzeugen.               | Ja                          |                                                                                                                                                       |
| Die Anwendung soll mit Einflussfaktoren die Dauer der simulierten Arbeitsgänge verlängern/verkürzen. | Ja                          |                                                                                                                                                       |
| Die Anwendung soll das hinterlegte Kausale Modell durch maschinelles Lernen wieder erzeugen können.  | Teilweise                   | Mittels Python-Bibliothek                                                                                                                             |

## Ausblick

Für die Weiterentwicklung dieses Forschungsprojektes kann die Simulation bzw. die Bibliothek weiterentwickelt werden. Es könnten neue Entitäten/Ressourcen für die Simulation hinzugefügt oder weitere Szenarien angelegt werden. Der Code selbst sollte einer sorgfältigen Qualitäts- und Performanceanalyse unterzogen werden. Es wurden Benchmarks während des Forschungsseminars angelegt, dennoch ist im Besonderen der implementierte Planungsalgorithmus (Giffler-Thompson-Algorithmus) unter bestimmten Umständen sehr langsam.

Die Bibliothek könnte weiterhin um eine grafische Benutzeroberfläche erweitert werden. Die Grundsteine dafür wurden bereits gelegt. (siehe [UI für einfache Erstellung von Konfigurationsdateien](application.md#ui-für-einfache-erstellung-von-konfigurationsdateien) Dieses UI könnte ausgebaut werden, sodass das Konfigurieren, Starten und Auswerten der Simulation alles in einer nativen Anwendung stattfinden kann.

Im Bereich des Structure Learning fehlt es dem Projekt noch an Erfahrung. Hier sollte es in Zukunft weitere Experimente mit den eingesetzten Variablen, der Diskretisierung einzelner Variablen geben oder weitere Algorithmen bzw. Bibliotheken ausprobiert werden. Die Basis für ein weiteres Vorgehen in diesem Bereich wurde bereits erschaffen.

In diesem Zuge könnte auch weiter mit stetigen Modellen experimentiert werden. Die Bibliothek bietet bereits die Möglichkeit, stetige Modelle für die Simulation zu nutzen. Structure Learning für stetige Modelle ist jedoch noch nicht implementiert und wäre ein großer Schritt in Richtung eines erfolgreichen Projektes.

Zu guter Letzt könnten auch Erkenntnisse aus realen Daten einer Produktion in die Simulation bzw. die dafür verwendeten Modelle einfließen.

<br /><br />

[Zurück zur Gliederung](../../readme.md)
