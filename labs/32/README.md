# Lab 32

Cílem tohoto labu je v Azure vytvořit service principal, který bude mít práva na pushování kontejnerů do Azure Container Registry.

> Tento lab může udělat jen ten, kdo má v Azure Active Directory práva na vytváření účtů (např. role Application Developer).

## Krok 1 - Založení service principala a jeho zadání do TeamCity

1. Přihlašte se v Azure CLI: `az login`

2. Přepněte se do správné subscription: `az account set --subscription <SUBSCRIPTION_NAME>`

3. V PowerShellu si do proměnných uložte název container registry: `$registryName = "<CONTAINER_REGISTRY_NAME>"`

4. Vymyslete si unikátní název service principala (měl by obsahovat slova TeamCity a deploy, aby bylo jasné, k čemu je): `$spName = "<SERVICE_PRINCIPAL_NAME>"`

5. Zjistěte unikátní cestu k Azure Container Registry:

```
$acrId = az acr show --name $registryName --query "id" --output tsv
write-host $acrId
```

6. Vytvořte service principala a uložte si jeho ID a heslo:

```
$spPassword = az ad sp create-for-rbac --name $spName --scopes "$acrId" --role acrpush --query "password" --output tsv
$spUserName = az ad sp list --display-name $spName --query "[].appId" --output tsv

write-host "User ID: $spUserName"
write-host "Password: $spPassword"
```

7. V TeamCity otevřete stránku nastavení projektu a v levém menu najděte sekci __Connections__.

8. Přidejte connection __Docker__ a zadejte adresu container registry (`<REGISTRY_NAME>.azurecr.io`) a vygenerované jméno a heslo.

## Krok 2 - Použití v rámci build konfigurace

1. V nastavení build konfigurace v sekci __Build Features__ přidejte feature __Docker Support__ a vyberte vytvořené spojení.

2. V sekci __Build steps__ přidejte další krok typu __Docker__. 

    * Tentokrát vyberte příkaz __tag__

    * Do parametrů příkazu dejte `northwindstoreapp:latest %dockerregistryurl%/northwindstore/app:%build.number%`

3. Uložte a v sekci __Parameters__ v levém menu zadefinujte parametr `dockerregistryurl`, kam dáte URL vašeho Docker registry (`<REGISTRY_NAME>.azurecr.io`).

4. Přidejte další krok __Docker__, tentokrát s příkazem __push__.

5. Odškrtněte volbu __Remove image from agent after push__.

6. Jako __Image name:tag__ zadejte `%dockerregistryurl%/northwindstore/app:%build.number%`

7. Uložte a vyzkoušejte, že se konfigurace provede a image se uloží do Azure Container Registry.

8. Upravte pipeline tak, aby s imagem publikovala i tag `latest`.