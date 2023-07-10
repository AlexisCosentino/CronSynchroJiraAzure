# CronSynchroJiraAzure

## Scan de Jira
1. N'importe quel statut vers "A valider" 
    - JIRA : RAS
    - AZURE :création ou changement du status en NEW + cocher la case "To Estimate".
2. N'importe quel statut vers "Acceptée" 
    - JIRA : RAS
    - AZURE : Création ou changement de statut en "New" + ajout du dernier commentaire dans la tache.
3. "A tester" vers "Test KO" dans un délais supérieur à 15 jours 
    - JIRA : changement de statut vers "Demande KO"
    - AZURE : RAS
4. "A tester" vers "Test KO" dans un délais inférieur ou égal à 15 jours 
    - JIRA : RAS
    - AZURE : Changement de statut vers "Back" + ajout du dernier commentaire dans la tache.
    
## Scan d'Azure (avec un Jira link présent uniquement)
1. N'importe quel statut vers "Done" avec "To estimate" de coché :
    - JIRA : changement de statut vers "A compléter" + ajout du dernier commentaire.
    - AZURE : RAS
2. N'importe quel statut vers "Done" avec "To estimate" NON coché :
    - JIRA : changement de statut vers "A tester" + ajout du dernier commentaire.
    - AZURE : RAS
3. N'importe quel statut vers "Removed":
    - JIRA : changement de statut vers " Rejeté" + ajout du dernier commentaire.
    - AZURE : RAS
    
## Scan les sprints d'Azure
1. Si une tache est affecté dans un sprint :
  - JIRA : Maj les dates de début et de fin avec les dates de sprint
  
2. Lors d'une cloture sprint, les taches qui ne sont pas en statut "DONE" :
  - JIRA : RAS
  - AZURE : la tache passe en statut "Closed Sprint", puis duplication de cette tache avec :
      - Nouvelle tache : 
          - Statut "To Do",
          - Remaining estime = original estimate,
          - Atribué à personne,
          - Titre [CLOSED SPRINT] + titre origine,
          - Iteration Backlog
          - Original estimate = original estimate de la tache origine - temps passé tache d'origine
          
          
# Le code

* Pour que l'app fonctionne, vous devez absolument avoir un fichier json contenant les mots de passe et login nécessaires, dans le dossier bin/DEBUG/, merci de me demander si vous en avez besoin.

Le coeur du programme se retrouve dans la page Program.cs
Le main appel un job qui est un Cron, qui est lancé actuellement toutes les minutes.

**Comment savoir si un ticket Jira existe sur Azure et si un ticket Azure existe dans Jira ??**
* Sur Jira, un champs AzureLink existe et doit etre remplis pour qu'il existe.
* Sur Azure, un champ LinkToJira existe et doit être remplis pour qu'il existe.

Le package Nugget NLog est utilisé afin d'écrire les logs sur un fichier externe, qui se trouve actuellement à la racine du projet : bin/DEBUG/SyncLogFile.log

Chaque énoncé ci-dessus à sa propre fonction que je vais vous expliquer.

## SyncJiraToAzure_Validate()
Fonction qui permet de gérer les tickets Jira dont le status est modifié en "A valider".
* Appel de la query sql et récupération d'une liste de JiraEntity contenant tout les tickets
* Pour chaque ticket dans la liste
* Je vérifie si le ticket existe sur Azure
    * Si non, 
        * Faire un post d'un nouveau ticket avec modification de sont status et ajout de "To estimate à True.
        * Ensuite j'insert into jira sql le lien du ticket azure dans le champs "linkToAzure".
        * Puis je requete les PJ et appel la fonction qui les post sur le PBI
    * Si oui,
        * Je fait un patch de celui ci avec un changement d'état et changement de to estimate en True.
    
## SyncJiraToAzure_Accepted()
Fonction qui permet de gérer les tickets jira dont le statut est modifié en "accepté".
* Appel de la query sql et récupération d'une liste de JiraEntity contenant tout les tickets
* Pour chaque ticket dans la liste
* Je vérifie si le ticket existe sur Azure
    * Si non, 
        * Faire un post d'un nouveau ticket avec modification de sont status en "New".
        * Appel de la fonction qui ajoute le dernier commentaire.
        * Requete des PJ et ensuite appel de la fonction qui les post sur celui ci.
        * Ensuite j'insert into jira sql le lien du ticket azure dans le champs "linkToAzure".
    * Si oui,
        * Je fait un patch de celui ci avec un changement d'état.
        * Appel de la fonction qui ajoute le dernier commentaire.
    
## SyncJira_KO()
Fonction qui permet de gérer les tickets jira dont le statut est modifié en "Test KO" et calcul combien de temps entre le changement d'état pour savoir si le chef de projet à gérer son ticket dans les temps ou pas.
* Appel de la query sql et récupération d'une liste de JiraEntity contenant tout les tickets KO depuis moins de 1h.
* Pour chaque ticket dans la liste
* Je calcul via une requete SQL le nombre de jour entre la modification d'etat de "Done" vers "Test KO"
    * Si nombre de jour <= 15 :
        * si le ticket existe sur Azure :
            * Patch de ce ticket avec changement d'état vers "Back - Test Failed"
            * Appel de la fonction qui ajoute le derneir commentaire.
    * Si nombre de jour > 15 :
        * Je fais un update SQL du ticket jira afin de modifier son etat vers "Demande KO".
        
## SyncAzure_Done() 
Fonction qui permet de récupérer les PBI azures dont le statut est devenu "DONE".
* Je fais un post vers azure avec une query afin de recuperer une liste de pbi dont statut = DONE, toestimate = true, jiralink existe et etat à changé aujourd'hui.
* Pour chaque pbi :
    * je le get via azure API
    * J'update le ticket sur JIRA avec le changement de statut vers "A compléter".
    * Je récupère l'ID jira de l'issue afin d'ajouter le dernier commentaire azure sur Jira.

* Je fais un nouveau post avec une nouvelle query pour recupérer une liste de pbi dont tout pareil mais estimate = false.
* Pour chaque pbi :
    * Je le GET via azure API
    * J'update le ticket Jira avec le changement de statut ver "A tester".
    * Je récupère l'ID jira de l'issue afin d'ajouter le dernier commentaire azure sur Jira.


## SyncAzure_Removed()
Fonction qui permet de recupérer les pbi dont le statut à changé dans la journée vers "REMOVED"
* Je fais un post avec query pour les recupérer.
* pour chaque pbi:
    * je les get via azure API
    * J'update le ticket JIRA en SQL avec le changement de statut.
    * Je récupère l'ID jira de l'issue afin d'ajouter le dernier commentaire azure sur Jira.

## SyncAzure_Sprint()
Fonction qui permet de modifier sur jira la date de début et la date de fin d'un ticket lorsque le PBI est affecté à un sprint sur Azure.
* J'itère sur chaque projet
    * Je get l'itération en cours
    * Je récupère la date de début, la date de fin, l'ID.
    * Je get les tickets associés à ce sprint en cours.
    * Si cette liste n'est pas vide :
        * pour chaque pbi :
            * Je get le ticket,
            * Je compare la date de début Jira et Azure, si elle est différente :
                * J'update la date de début de JIRA avec celle d'Azur.
            * Je compare la date de fin de Jira et Azure, si elle est différente :
                * J'update la date de fin JIRA avec celle d'Azure.
               
## SyncAzure_ClosedSprint()
Cette fonction sert à automatiser une/des actions sur une tache lors d'une cloture de sprint AZURE.
* J'itère sur chaque projet
* Je recupère les itérations du projet,
* Je récupère l'ID de l'itération passé la plus récente.
(Ceci est le seul moyen de récupérer le dernier sprint passé)
* Je GET la liste de PBI de cette itération.
* Pour chaque PBI de l'itération :
    * Je get le PBI en question,
    * Si ce PBI est une tache, Statut != Done && statut != Closed Sprint
        * Je duplique le pbi en faisant un post d'une nouvelle tache avec les meme data que le dernier pbi
        * Je patch l'ancien pour changer son etat en "Closed Sprint"
        * Je get la hierarchie du pbi closed et le patch sur le nouveau
        * Je get les commentaires et les post sur le nouveau

# Le déploiement

Le programme a été généré en exe/msi via visual studio. Il est installé et déployé sur notre serveur. 
Si il faut le réinstaller, voici la procèdure :
* Bien vérifier que le programme soit bien désinstallé sur le serveur en regardant dans application/fonctionnalités de windows.
* Ensuite installer le .msi
* ajouter dans 'c:\Program File (x86)\Irium Software\Cron SyncJiraAzure', le script powershell script-automate-restart.ps1
* Faire en sorte que le script powershell soit executé toutes les heure par le serveur, via une tâche planifié :
    * windows + r, tapez taskschd.msc
    * Creer une tâche
    * Lui donner un nom et description, cocher "Exécuter avec les autorisations maximales"
    * Déclencheur : Une fois et répéter la tache toutes les heures indéfiniment
    * Actions :
        * Démarrer un programme
        * Programme/script : C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe
        * Ajouter des arguments : -ExecutionPolicy Bypass -File "C:\Program Files (x86)\Irium Software\CRON SyncJiraAzure\script-automate-restart.ps1"
        * Commencer dans : C:\Program Files (x86)\Irium Software\CRON SyncJiraAzure

**TERMINE**


Lorsque le cron tourne, dans son dossier, vous trouverez un fichier de log nommé SyncLogFile.txt

Il y a également un deuxième fichier de logs créé par le script powershell qui annonce chaque heure si le programme tourne ou si il a été redémarré, il se nomme program-restart-log.txt