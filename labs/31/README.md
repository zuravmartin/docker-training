# Lab 31

Cílem tohoto labu je připravit Dockerfile pro projekty, které obsahují testy, a nachystat `docker-compose.ci.yml`, který umožní rozběhnutí aplikace a SQL databáze v kontejneru.

## Krok 0 - Fork repozitáře

1. Na stránce https://github.com/tomasherceg/docker-training si kliknutím na tlačítko __Fork__ vytvořte svůj fork repozitáře - budete do něj muset commitovat změny, aby je CI vidělo.

2. Naklonujte si váš fork vedle stávajícího adresáře (máme v něm nějaké změny, které budeme chtít přenášet do vlastního forku).

3. Nyní budeme pracovat ve vašem forku - ujistěte se, že jste ve větvi `day4`

## Krok 1 - Unit test project

1. Z větve `day4` vytvořte větev `day4-ci` - v ní budeme chystat úpravy aplikace pro zprovoznění CI.

2. Otevřete ve Visual Studiu hlavní solution `src/NorthwindStore.sln`.

3. Pravým tlačítkem klikněte na projekt `NorthwindStore.Tests.UnitTests` a vyberte možnost __Add > Docker Support__.

4. Vygenerovaný Dockerfile nedělá to, co bychom potřebovali.

    * Zrušte v něm všechny reference na runtimový kontejner - ten nepotřebujeme. Stačí nám stage `build`, která se odehrává v kontejneru SDK.

    * `dotnet build` nemusí výstupy generovat do adresáře `/app/build` - odeberme tento option.

    * `dotnet build` nemusí mít konfiguraci `Release`.

    * Před `dotnet restore` doplňme nakopírování adresáře `packages`, podobně jako u aplikačního Dockerfile.

    * Po příkazu `dotnet build` máme vše hotovo, budeme chtít akorát nastavit jako entry point `dotnet test NorthwindStore.Tests.UnitTests.csproj --no-build`.

4. Pravým tlačítkem klikněte na `Dockerfile` a vyberte možnost __Build Docker Image__.

5. Následujím příkazem kontejner spusťte a ověřte, že testy projdou.

```
docker run --rm northwindstoretestsunittests
```

## Krok 2 - Integration test project

1. Stejným postupem jako v kroku 1 vytvořte `Dockerfile` pro integrační testy v projektu  `NorthwindStore.Tests.IntegrationTests`.

2. Abychom mohli integrační testy spustit lokálně, měl by kontejner integračních testů vidět na databázový kontejner. Ten je hostován v Docker network vytvořené pomocí Docker compose - tato síť má ale automaticky generovaný název. Do `docker-compose.override.yml` tedy doplňte tuto instrukci, která založí síť s explicitním názvem `northwindstore-dev`:

```
networks:
  northwindstore-dev:
    name: northwindstore-dev
```

3. Oběma službám v `docker-compose.override.yml` doplňte instrukci, která je do této sítě přiřadí:

```
    networks:
    - northwindstore-dev
```

4. Pomocí __Rebuild solution__ nebo spuštění projektu se ujistěte, že se kontejnery vytvořily znovu a že jsou ve správné network. Jde to udělat například pomocí příkazu `docker inspect`.

5. Pravým tlačítkem klikněte na `Dockerfile` a vyberte __Build Docker Image__.

6. Ověřte, že kontejner s testy funguje:

```
docker run --rm -e "ConnectionStrings__DB=Data Source=tcp:northwindstore.db,1433; Initial Catalog=Northwind; User ID=sa; Password=DevPass_1" --network northwindstore-dev northwindstoretestsintegrationtests
```

## Krok 3 - Příprava docker-compose.ci.yml

V rámci buildu budeme chtít vytvořit kontejnery pro aplikaci i pro databázi, a zároveň je budeme chtít spustit. Proti aplikaci můžeme chtít v budoucnu spouštět UI testy (např. pomocí knihovny Selenium). Databázi zase budeme potřebovat na integrační testy.

1. Do adresáře `ci` zkopírujte soubor `src/docker-compose.override.yml` a pojmenujte jej jako `docker-compose.ci.yml`

2. Prověďte v něm tyto změny:

    * Odstraňte z proměnné `ASPNETCORE_URLS` URL pro https - v buildu nechceme řešit development HTTPS certifikáty.

    * Odstraňte ze sekce ports port `443`.

    * U aplikace úplně odeberte sekci `volumes`.

    * U databáze zrušte sekci `ports` - nechceme port exposovat navenek, pokud by na jednom stroji běželo více buildů najednou, došlo by ke kolizi.

    * U databáze zrušte sekci `volume` - zde si chceme databázi vždy inicializovat nanovo.

3. Commitněte a pushněte.

4. V TeamCity vytvořte novou build konfiguraci a namapujte ji na váš fork repozitáře.

5. Upravte její Version Control Setting nastavení tak, aby ve výchozím stavu buildovala větev `refs/heads/day4-ci`.

6. Jako první build step přidejte __Command line__ a jako skript ke spuštění zadejte tento příkaz:

```
docker-compose -f src/docker-compose.yml -f ci/docker-compose.ci.yml up --build --force-recreate -d
```
> Option `--build` řekne, že se před startem kontejnerů má provést build.
>
> Option `--force-recreate` smaže kontejnery, pokud tam existovaly, a vytvoří je znovu.
>
> Option `-d` je důležitý - nezablokuje nám terminál tím, že bude čekat, než kontejnery skončí.
>
> Pozor, záleží na pořadí optionů a příkazu `up`. Před něj se dávají parametry `-f`, které jsou obecné bez ohledu na příkaz `up`. Na konec se dávají optiony, které jsou specifické pro příkaz `up`.
>
> TeamCity obsahuje i krok _Docker Compose_, ale ten nemůžeme použít - nemá možnosti přidávat vlastní command-line options, takže kontejnery v dockerfile jen spustí a neprovede kompilaci samotnou.

7. Spusťte build a ověřte, že vše doběhnulo.

## Krok 4 - Spuštění testů

První krok v pipeline nám ověřil, že zdrojové kódy jdou zkompilovat, že umíme postavit kontejner s vývojářskou databází a že to celé jde spustit. Nyní je čas na testy.

1. Přidejte do pipeline krok __Docker__ a ponechte command __build__.

2. Vyberte cestu k Dockerfile `src/NorthwindStore.Tests.UnitTests/Dockerfile`

3. __Context folder__ bude `src`

4. Image name a tag zadejme `northwindstoretestsunittests:%build.number%` - image dostane unikátní číslo.

5. __Additional arguments__ můžeme smazat.

6. Uložte a potvrďte.

7. Přidejte do pipeline další krok __Command line__.

> Zde nechceme použít command __Docker__, ten se nehodí pro spouštění `docker run`. Namísto toho používáme funkcionalitu __Docker wrapper__, která umožní spustit kontejner tak, aby viděl na environment proměnné build pipeline a zároveň jeho filesystém bude namapován do adresáře, ve kterém agent pracuje, takže pokud do něj něco zapíšeme, po skončení kontejneru to tam build agent uvidí.

8. Jako __Run__ vyberte __Executable with parameters__

9. __Command executable__ bude `dotnet`

10. __Command parameters__ dejme `test /src/NorthwindStore.Tests.UnitTests/NorthwindStore.Tests.UnitTests.csproj --no-build`

11. Do pole __Run step within Docker container__ zadejte název kontejneru, tedy `northwindstoretestsunittests:%build.number%`

12. Ověřte, že pipeline projde a že se testy spustí.

## Krok 5 - Přidání logování

Příkazu `dotnet test` můžeme dát parametry, které mu přikážou, aby vytvořil XML soubor s výsledky testů.

1. Build stepu, který testy spouští, přidejte na konec do argumentů následující konstrukci:

```
 --logger trx;LogFileName=%teamcity.build.checkoutDir%/testresults/unittests-results.trx
```

> Používáme zde proměnnou `%teamcity.build.checkoutDir%`, která obsahuje cestu k adresáři, ve kterém má build agent namapované zdrojové kódy. Tuto složku nám TeamCity do kontejneru dostane jako volume, a cestu k ní nám dá do této proměnné.

2. V menu nalevo otevřete sekci __Build features__ a přidejte jako funkci __XML report processing__.

3. Jako sledovanou cestu jí zadejte `+:testresults/*.trx`

4. Spusťte build a ověřte, že agent najde soubor s výstupem textů a zobrazí ve výsledcích 1 úspěšný test.

## Krok 6 - Přidání integračních testů

1. Stejným způsobem udělejte build a spuštění integračních testů.

    * Kontejner bude pro spouštění integračních testů bude potřebovat environment variable `ConnectionStrings__DB=Data Source=tcp:northwindstore.db,1433; Initial Catalog=Northwind; User ID=sa; Password=DevPass_1`

    * Přidejte ještě parametr `--network northwindstore-dev`, aby kontejner viděl na databázi.
    
    * Jako název kontejneru dejte `northwindstoretestsintegrationtests:%build.number%` 

## Krok 7 - Úklid

1. Jako poslední krok pipeline přidejte opět __Command line__ step, který zabije kontejnery vytvořené a spuštění pomocí `docker-compose`:

```
docker-compose -f src/docker-compose.yml -f ci/docker-compose.ci.yml down
```

2. Klikněte na __Show advanced options__ a u pole __Execute step__ vyberte možnost __Always, even if build stop command was issued__.

