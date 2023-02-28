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

Le coeur du programme se retrouve dans la page Program.cs
Le main appel un job qui est un Cron, qui est lancé actuellement toutes les minutes.
**Pour le moment le cron est configuré chaque minutes mais peut évidemment être modifié à notre guise, ne pas oublier de modifier chaque query sql également**

**Comment savoir si un ticket Jira existe sur Azure et si un ticket Azure existe dans Jira ??**
* Sur Jira, un champs AzureLink existe et doit etre remplis pour qu'il existe.
* Sur Azure, un champ LinkToJira existe et doit être remplis pour qu'il existe.

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
* Je fais un nouveau post avec une nouvelle query pour recupérer une liste de pbi dont tout pareil mais estimate = false.
* Pour chaque pbi :
    * Je le GET via azure API
    * J'update le ticket Jira avec le changement de statut ver "A tester".

## SyncAzure_Removed()
Fonction qui permet de recupérer les pbi dont le statut à changé dans la journée vers "REMOVED"
* Je fais un post avec query pour les recupérer.
* pour chaque pbi:
    * je les get via azure API
    * J'update le ticket JIRA en SQL avec le changement de statut.

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

# Informations
* Ajout de commentaire de Azure -> Jira PAS FAIT, ne sait pas comment faire proprement.
* Ajout de PJ Azure -> Jira PAS FAIT, ne sait pas comment faire du tout.
* Vérifier le temps dans chaque query
* Pour que l'app fonctionne, vous devez absolument avoir un fichier json contenant les mots de passe et login nécessaires, dans le dossier bin/DEBUG/, merci de me demander si vous en avez besoin.