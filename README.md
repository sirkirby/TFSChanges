TFSChanges v1.1.0
==========

A simple yet extensible console app that polls the visualstudio.com tfs odata api for changeset and build changes. When changes are found they are distributed to implemented provider channels. Right now, this is for my needs only but I've been able to abstract out most of the specific bits to make the code more useful for others with different needs. TFS does have a mechanism for posting SOAP updates for specific events, but due to its complexity and limitations, polling the new odata api was much easier and more effective. I'm hoping they will eventually support better push mechanisms making the need for a solution like this obsolete.

Configure
---------
Create a new table on your Azure storage account and put the name of that table in the [App.config](TFSChanges/App.config)

    <setting name="StorageTableName" serializeAs="String">`
    	<value>TFSIntegration</value>
    </setting>

You need a minimum of two rows that which have columns that match the [Preferences](TFSChanges/Preferences.cs) class. Azure tables can have any structure, but for the automatic serialization and deserialization to work, you need those class properties at minimum. The PartitionKey is static, `TFSChanges` for production and `TFSChanges_Test` for testing. The RowKey should match the [assembly](TFSChanges/Properties/AssemblyInfo.cs) version, ex `1.1.0.0`

- LastChecked (used as the placeholder for polling)
- TfsUser (tfs service username **you must enable alternate credentials in your profile, this is not your ms account id**)
- TfsPassword (password specified in tfs online profile alt credentials)
- TfsApiUri (https://tfsodata.visualstudio.com/DefaultCollection/)
- TfsUri (your tfs root url, https://mytfsaccount.visualstudio.com/)
- AlmTaskUri (The base url to a referenced task id in your checkin comment)
- AlmTaskExpression (regex to identify alm task id in comment, ex `[A-Z]+\-[0-9]+`)
- HipChatAuthToken (your v1 hipchat api auth token used for the HipChat Provider)
- Projects (json array of [TfsProject](TFSChanges/Models/TfsProject.cs) types. This is the configuration used to target specific projects in TFS and specify output channel preferences...needs work as other providers are added)

        [{
            "Name":"MyTeamProjectName",
            "ProductionUri":"http://projectwebsite.com",
            "StagingUri":"http://staging.productwebsite.com",
            "HipChatRoomId":123456,
            "HipChatNotify":true,
            "HipChatChangesetColor":"yellow",
            "HipChatBuildColor":"green",
            "HipChatBuildFailedColor":"red",
            "IsActive":true
        }]

Build
-----
Using msbuild with your favorite build server. This is not set up for solution package restore, so you need to add the necessary build step to restore the packages prior to your msbuild step.

Run
-----------
The app by default is configured by default to use Azure table storage configured above. You must specify the storage account name and the key via the command line or a arguments.json file. The arguments file is simply a json serialized version of the [Arguments](TFSChanges/Models/Arguments.cs) class.

Example command args

    TFSChanges.exe /sa "myazaccount" /sk "512-bit storage access key" /debug true

For information on how to get your storage account access key visit https://www.windowsazure.com/en-us/manage/services/storage/how-to-manage-a-storage-account/#regeneratestoragekeys

Example arguments.json

    {
    	"StorageAccount": "mystorageaccountname",
    	"StorageKey": "512-bit storage access key",
    	"Debug": true
    }

The arguments.json file must be in the same directory as the TFSChanges.exe

Now schedule the app to run as often as you want to check for changes. I'm currently using Windows Task Scheduler.

Requirements
------------
- .Net Framework 4.5

License
-------
[The MIT License (MIT)](LICENSE.txt)
