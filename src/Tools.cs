using System.Net;
using System.Xml;
using dragonrescue.Api;
using dragonrescue.Util;
using dragonrescue.Schema;

namespace dragonrescue;
public class Tools {
    public static async System.Threading.Tasks.Task SelectDragon(LoginApi.Data loginData, int raisedPetID, int petTypeID) {
        (var client, var apiToken, var profile) = await LoginApi.DoVikingLogin(loginData);
        
        var formContent = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("apiKey", Config.APIKEY),
            new KeyValuePair<string, string>("apiToken", apiToken),
            new KeyValuePair<string, string>("raisedPetID", raisedPetID.ToString()),
        });
        var bodyRaw = await client.PostAndGetReplayOrThrow(Config.URL_CONT_API + "/ContentWebService.asmx/SetSelectedPet", formContent);
        Config.LogWriter(string.Format("SetSelectedPet for {0}: {1}", raisedPetID, bodyRaw));
        
        string keyName;
        if (Config.APIKEY == "1552008f-4a95-46f5-80e2-58574da65875"){
           keyName = "CurrentRaisedPetType";
        } else if (Config.APIKEY == "6738196d-2a2c-4ef8-9b6e-1252c6ec7325"){
            keyName = "MBCurrentRaisedPetType";
        } else {
            return;
        }
        formContent = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("apiKey", Config.APIKEY),
            new KeyValuePair<string, string>("apiToken", apiToken),
            new KeyValuePair<string, string>("pairId", "1967"),
            new KeyValuePair<string, string>("contentXML", 
                "<?xml version=\"1.0\"?><Pairs xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><Pair><PairKey>" + keyName +
                "</PairKey><PairValue>" + petTypeID.ToString() + "</PairValue><UpdateDate>0001-01-01T00:00:00</UpdateDate></Pair></Pairs>"),
        });
        bodyRaw = await client.PostAndGetReplayOrThrow(Config.URL_CONT_API + "/ContentWebService.asmx/SetKeyValuePair", formContent);
        Config.LogWriter(string.Format("Set {0} to {1}: {2}", keyName, petTypeID, bodyRaw));
    }
    
    private static async System.Threading.Tasks.Task<bool> BoolRequest(HttpClient client, string url, FormUrlEncodedContent request) {
        bool success = false;
        var bodyRaw = await client.PostAndGetReplayOrThrow(Config.URL_CONT_API + url, request);
        try {
            success = XmlUtil.DeserializeXml<bool>(bodyRaw);
        } catch {}
        return success;
    }
    
    public static async System.Threading.Tasks.Task RemoveDragon(LoginApi.Data loginData, string dragonId) {
        Console.WriteLine($"Removing Dragon: {dragonId}");
        
        (var client, var apiToken, var profile) = await LoginApi.DoVikingLogin(loginData);
        
        var removeRequest = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("apiKey", Config.APIKEY),
            new KeyValuePair<string, string>("apiToken", apiToken),
            new KeyValuePair<string, string>("raisedPetID", dragonId),
        });

        // unselect if is selected
        bool success = await BoolRequest(client, "/ContentWebService.asmx/SetRaisedPetInactive", removeRequest);
        Console.WriteLine($"Dragon unselected: {success}");
        
        // convert to fish
        var fishRequest = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("apiKey", Config.APIKEY),
            new KeyValuePair<string, string>("apiToken", apiToken),
            new KeyValuePair<string, string>("raisedPetData", 
                $"<?xml version=\"1.0\" encoding=\"utf-8\"?> <RPD> <id>{dragonId}</id> <ptid>2</ptid> <is>false</is> <ir>false</ir> <gd>0</gd> <updt>0001-01-01T00:00:00</updt> </RPD>"
            ),
        });
        success = await BoolRequest(client, "/ContentWebService.asmx/SetRaisedPet", fishRequest);
        Console.WriteLine($"Dragon converted into Fish: {success}");
        if (!success) {
            Console.WriteLine("Removing Dragon: failed");
            return;
        }
        
        // remove
        success = await BoolRequest(client, "/ContentWebService.asmx/SetRaisedPetInactive", removeRequest);
        Console.WriteLine($"Minisaur killed: {success}");
        if (!success) {
            Console.WriteLine("Removing Dragon: Failed");
            return;
        }
    }
    
    public static async System.Threading.Tasks.Task ReplaceDragon(LoginApi.Data loginData, string path, string img1File = "-", string img2File = "-") {
        XmlDocument dragon = new XmlDocument();
        dragon.PreserveWhitespace = true;
        dragon.Load(path);
        
        (var client, var apiToken, var profile) = await LoginApi.DoVikingLogin(loginData);
        
        // get current pet
        var oldPetStr = await client.PostAndGetReplayOrThrow(
            Config.URL_CONT_API + "/ContentWebService.asmx/GetSelectedRaisedPet",
            new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("apiKey", Config.APIKEY),
                new KeyValuePair<string, string>("apiToken", apiToken),
            })
        );
        Config.LogWriter($"Pet {oldPetStr} will be replaced");
        var oldPet = XmlUtil.DeserializeXml<RaisedPetData[]>(oldPetStr);
        
        // update input xml
        dragon["RaisedPetData"]["uid"].InnerText = oldPet[0].UserID.ToString();
        dragon["RaisedPetData"]["eid"].InnerText = oldPet[0].EntityID.ToString();
        dragon["RaisedPetData"]["id"].InnerText  = oldPet[0].RaisedPetID.ToString();
        dragon["RaisedPetData"]["ip"].InnerText  = oldPet[0].ImagePosition.ToString();
        
        // update xml on server
        var setRaisedPetRequest = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <RPR xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"> <ptid>0</ptid> <rpd> " + dragon["RaisedPetData"].InnerXml + " </rpd> </RPR>";
        
        string res = await DragonApi.SetRaisedPet(client, apiToken, setRaisedPetRequest);
        Config.LogWriter($"Replace pet xml: {res}");
        
        // update images
        if (oldPet[0].ImagePosition != null) {
            int imagePosition = (int)(oldPet[0].ImagePosition);
            
            if (img1File != "-" && File.Exists(img1File)) {
                string imgData = Convert.ToBase64String(System.IO.File.ReadAllBytes(img1File));
                res = await ImageApi.SetImage(client, apiToken, imagePosition, imgData, "EggColor");
                Config.LogWriter($"Replace pet EggColor (imagePosition={imagePosition}): {res}");
            }
            
            if (img2File != "-" && File.Exists(img1File)) {
                string imgData = Convert.ToBase64String(System.IO.File.ReadAllBytes(img2File));
                res = await ImageApi.SetImage(client, apiToken, imagePosition, imgData, "Mythie");
                Config.LogWriter($"Replace pet Mythie (imagePosition={imagePosition}): {res}");
            }
        }
    }
}
