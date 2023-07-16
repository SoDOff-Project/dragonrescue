using System.Net;
using System.Xml;
using dragonrescue.Schema;
using dragonrescue.Util;

namespace dragonrescue.Api;
public static class DragonApi {
    public static async Task<string> CreatePet(HttpClient client, string apiToken, string request) {
        var formContent = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("apiKey", Config.APIKEY),
            new KeyValuePair<string, string>("apiToken", apiToken),
            new KeyValuePair<string, string>("request", request),
        });

        var response = await client.PostAsync(Config.URL_CONT_API + "/V2/ContentWebService.asmx/CreatePet", formContent);
        var bodyRaw = await response.Content.ReadAsStringAsync();
        response.Dispose();
        return bodyRaw;
    }

    public static async Task<string> SetRaisedPet(HttpClient client, string apiToken, string request) {
        var formContent = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("apiKey", Config.APIKEY),
            new KeyValuePair<string, string>("apiToken", apiToken),
            new KeyValuePair<string, string>("request", request),
        });

        var response = await client.PostAsync(Config.URL_CONT_API + "/v3/ContentWebService.asmx/SetRaisedPet", formContent);
        var bodyRaw = await response.Content.ReadAsStringAsync();
        response.Dispose();
        return bodyRaw;
    }

    public static async Task<string> SetImage(HttpClient client, string apiToken, int imageSlot, string image) {
        var formContent = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("apiKey", Config.APIKEY),
            new KeyValuePair<string, string>("apiToken", apiToken),
            new KeyValuePair<string, string>("ImageType", "EggColor"),
            new KeyValuePair<string, string>("ImageSlot", imageSlot.ToString()),
            new KeyValuePair<string, string>("contentXML", "<ImageData xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"> <ImageURL /> <TemplateName>T</TemplateName> </ImageData>"),
            new KeyValuePair<string, string>("imageFile", image.ToString()),
        });

        var response = await client.PostAsync(Config.URL_CONT_API + "/ContentWebService.asmx/SetImage", formContent);
        if (response.StatusCode != HttpStatusCode.OK) {
            response.Dispose();
            response = await client.PostAsync(Config.URL_CONT_API + "/v3/ContentWebService.asmx/SetImage", formContent);
        }
        var bodyRaw = await response.Content.ReadAsStringAsync();
        response.Dispose();
        return bodyRaw;
    }


    public static async Task<string> CreateDragonFromXML(HttpClient client, string childApiToken, string profileID, XmlNode raisedPetData, string? imageData) {
        // create "template" pet - chimeragon ;-)
        
        Thread.Sleep(Config.NICE);
        
        var createPetRequest = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <RPR xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"> <ptid>111</ptid> <SASP>false</SASP> <USOP>false</USOP> <pgid>9</pgid> <cir> <iid>18589</iid> <cid>11</cid> <q>-1</q> </cir> <rpd> <id>0</id> <uid>" + profileID + "</uid> <ptid>111</ptid> <gs> <id>593</id> <n>Hatching</n> <ptid>111</ptid> <o>1</o> </gs> <g>RS_DATA/DragonEgg.unity3d/PfEggChimeragon</g> <t /> <gd>1</gd> <at> <k>HatchTime</k> <v>7/15/2023 7:52:56 AM</v> <dt>11</dt> </at> <at> <k>Priority</k> <v>0</v> <dt>7</dt> </at> <at> <k>FoodEffect</k> <v /> <dt>11</dt> </at> <at> <k>IncubatorID</k> <v>1</v> <dt>7</dt> </at> <at> <k>NameCustomized</k> <v>False</v> <dt>1</dt> </at> <is>false</is> <ir>false</ir> <cdt>0001-01-01T00:00:00</cdt> <updt>0001-01-01T00:00:00</updt> <ispetcreated>false</ispetcreated> </rpd> </RPR>";
        var newPet = await CreatePet(client, childApiToken, createPetRequest);
        CreatePetResponse newPetXml = XmlUtil.DeserializeXml<CreatePetResponse>(newPet);
        
        // update original dragon
        
        // avoid locked dragons in Edge
        for (int i = 0; i < raisedPetData.ChildNodes.Count; i++) {
            if (raisedPetData.ChildNodes[i].Name == "at" && raisedPetData.ChildNodes[i]["k"].InnerText == "TicketID") {
                raisedPetData.RemoveChild(raisedPetData.ChildNodes[i]);
            }
        }
        
        // use new (emu) ids
        raisedPetData["uid"].InnerText = profileID;
        raisedPetData["eid"].InnerText = newPetXml.RaisedPetData.EntityID.ToString();
        raisedPetData["id"].InnerText  = newPetXml.RaisedPetData.RaisedPetID.ToString();
        raisedPetData["ip"].InnerText  = newPetXml.RaisedPetData.ImagePosition.ToString();
        
        // replace pet by original dragon
        
        Thread.Sleep(Config.NICE);
        
        var setRaisedPetRequest = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <RPR xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"> <ptid>0</ptid> <rpd> " + raisedPetData.InnerXml + " </rpd> </RPR>";
        
        var res = await SetRaisedPet(client, childApiToken, setRaisedPetRequest);
        
        // set image
        
        Thread.Sleep(Config.NICE);
        
        if (imageData == null) {
            imageData = "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAKBueIx4ZKCMgoy0qqC+8P//8Nzc8P//////////////////////////////////////////////////////////2wBDAaq0tPDS8P//////////////////////////////////////////////////////////////////////////////wAARCAEAAQADASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwBlFFFSUFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAf/Z";
        }
        await SetImage(client, childApiToken, newPetXml.RaisedPetData.ImagePosition.Value, imageData);
        
        return res;
    }
}
