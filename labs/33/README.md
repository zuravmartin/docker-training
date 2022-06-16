# Lab 33

Cílem tohoto labu je vytvořit pipeline, která bude nasazovat aplikaci automaticky do testovacího prostředí.

## Krok 1 - Uložení helm chartu jako build artifact

1. Do složky `ci` zkopírujte z původního repozitáře finální Helm chart (složku `app`), který jsme vytvářeli v labu 24.

> Soubor `Chart.yaml` by měl být na cestě `ci/app/Chart.yaml`.

2. Commitněte a pushněte změnu.

3. Upravte build konfiguraci, která provádí build a publish kontejneru - v levém menu vyberte záložku __General settings__.

4. Do pole __Artifact paths__ zadejte hodotu `ci/app => app` (složku `ci/app` chceme mít v artifactu jako složku `app`).

5. Spusťte pipeline a zkontrolujte, že je helm chart v artifactu.

## Krok 2 - Vytvoření deployment konfigurace

> Nejprve v sekci __Administration__ na stránce __Plugins__ ověřte, že je instalován plugin __Helm support__. Pokud ne, místo toho použijte krok command-line.

1. V projektu založte druhou build konfiguraci s názvem __CD__ a jako její typ vyberte __Deployment__.

2. Po založení v levém menu přejděte do sekce __Dependencies__ a přidejte build konfiguraci __CI__ jako __Artifact Dependency__.

    * Do __Artifact rules__ zadejte cestu `app => app`.

    * Pro jistotu vyberte __Clean destination paths before downloading artifacts__.

3. Do __Build steps__ přidejte první krok typu __Command line__.

4. Jako skript zadejte hodnotu:

```
az login --service-principal -u "%spusername%" -p "%sppassword%" --tenant "%sptenant%"
az aks get-credentials -n "%clustername%" -g "%resourcegroup%"
```

5. V sekci __Parameters__ doplňte hodnoty použitých parametrů:

    * Jméno service principala
    
    * Heslo service principala (klikněte na tlačítko __Spec__ a označte jej jako __password__)
    
    * Název AKS clusteru
    
    * Název resource group, ve které se cluster nachází

    * Tenant pro service principala zjistíte příkazem `az ad sp list --display-name $spName --query "[].appOwnerOrganizationId" --output tsv`

6. Do __Build steps__ přidejte první krok typu __Helm__.

    * Jako příkaz vyberte __Upgrade__

    * Release name jsme minule dávali `helm`

    * Cesta k chartu by měla být `app/`

    * Jako další command-line parametry zadejte `-i --create-namespace --namespace northwindstore-helm --set ingress.hosts[0].host=%appurl% --set ingress.hosts[0].paths[0].path=/ --set ingress.hosts[0].paths[0].pathType=Prefix --set ingress.tls[0].hosts[0]=%appurl%`

7. Nadefinujte parametr `appurl` s hodnotou `helm.northwindstore.local`

## Krok 3 - Pipeline je připravena ke spuštění, ale nemáme práva

Nyní je ještě třeba service principalovi přidat práva na management clusteru. Má práva jen do Azure Container Registry.

1. Nejprve je třeba zjistit identifikátor clusteru:

```
$clusterId = az aks show -g <RESOURCE_GROUP> -n <CLUSTER_NAME> --query id -o tsv
write-host $clusterId
```

2. Dále je třeba zjistit Object ID service principala:

```
$spObjectId = az ad sp list --display-name $spName --query "[].id" --output tsv
write-host $spObjectId
```

3. Nyní je třeba service principala přidat do Cluster User Role - té je pak možné přiřazovat práva na úrovni prostředků v Kubernetes (RBAC):

```
az role assignment create --assignee $spObjectId --role "Azure Kubernetes Service Cluster User Role" --scope $clusterId
```

4. Ve složce `ci` založte podsložku `rbac` a vytvořte v ní soubor `role-northwindstore-helm.yml` s tímto obsahem:

```
kind: Role
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: northwindstore-helm-role
  namespace: northwindstore-helm
rules:
- apiGroups: ["", "extensions", "apps"]
  resources: ["*"]
  verbs: ["*"]
- apiGroups: ["batch"]
  resources:
  - jobs
  - cronjobs
  verbs: ["*"]
---
kind: RoleBinding
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: northwindstore-helm-role-binding
  namespace: northwindstore-helm
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: Role
  name: northwindstore-helm-role
subjects:
- kind: Group
  namespace: dev
  name: <SERVICE_PRINCIPAL_OBJECT_ID>
```

> První objekt definuje roli `northwindstore-helm-role`, která dává plná práva na namespace `northwindstore-helm`.
>
> Druhý objekt definuje vazbu mezi touto rolí a objektem v Azure Active Directory.

5. Nasaďte tento objekt do Kubernetes:

```
kubectl apply -f role-northwindstore-helm.yml
```

6. Nyní zkuste build konfiguraci spustit.

## Krok 4 - Dosazení správného čísla verze

Pokud se podíváte do clusteru na detaily podu (`kubectl describe <POD_NAME>`), zjistíte, že je stále nasazen kontejner ve verzi `1.0`.

1. Přidejte do command-line argumentů u příkazu __Helm__ následující nastavení:

```
--set image.tag=%dep.<YOUR_DEPENDENT_CONFIG_NAME>.build.number%
```

2. Nyní zkuste build konfiguraci spustit a ověřit, že je vše v pořádku.

> V reálném světě by bylo vhodné přidat ještě další krok, který po deploymentu ověří, že je vše v pořádku - například pingne web pomocí `curl`. Kvůli tomu, že aplikace běží na doméně `.local`, není to dost dobře možné.