module Tests

open ArrangementService.Utils
open ArrangementService.Event.Models
open ArrangementService.UserMessage
open type ArrangementService.Event.WriteModel
open Expecto
open System


let compareListOfRecords a b = Seq.zip a b |> Seq.map (fun (x,y) -> Expect.equal x y "These should be equal") |> ignore



let newFunc event participants =  
  let result =  {
    ArrangementService.Participant.ParticipantsWithWaitingList.attendees = participants
    ArrangementService.Participant.ParticipantsWithWaitingList.waitingList = Seq.empty<ArrangementService.DomainModels.Participant>
  }
  let funcCall = ArrangementService.Participant.Service.getParticipantsForEventPure event participants
  compareListOfRecords funcCall.attendees result.attendees
  compareListOfRecords funcCall.waitingList result.waitingList

let testGetParticipantsForEvent = 
  let writeModel = { 
      ArrangementService.Event.WriteModel.Title = "String"
      Description = "String"
      Location =  "String"
      OrganizerName =  "String"
      OrganizerEmail =  "String@"
      MaxParticipants =  10
      StartDate = {
                  Date = {
                  Day = 1
                  Month = 1
                  Year = 1970
                  };
                Time = {
                        Hour = 1
                        Minute = 1
                }
      }
      EndDate =  {
                  Date = {
                  Day = 2
                  Month = 1
                  Year = 1970
                 };
                  Time = {
                          Hour = 1
                          Minute = 1
                  };
      }
      OpenForRegistrationTime = "1624266180000"
      editUrlTemplate  = "hei"
      ParticipantQuestion = "hei"
      HasWaitingList = true
    }
  let write = {
    ArrangementService.Participant.WriteModel.name = "Hei"
    ArrangementService.Participant.WriteModel.comment = "Hei"
    ArrangementService.Participant.WriteModel.cancelUrlTemplate = "Hei"
  }
  let getMessage (u:UserMessage):string = match u with
                                          |BadInput m -> m
                                          |_  -> "Something went wrong"

  let eventResult = writeToDomain (Guid.NewGuid()) writeModel (Guid.NewGuid()) 
  let participant = ArrangementService.Participant.Models.writeToDomain (Guid.NewGuid(), "email@key.com") write
  match (eventResult, participant) with 
      |(Ok event, Ok participant) -> newFunc event (seq{participant})
      |(Error exp, _) -> failwith (getMessage exp.Head)
      |(_, Error _) -> failwith "Participant was wrongly formatted"


[<Tests>]
let tests =
  testList "samples" [
    testCase "universe exists (╭ರᴥ•́)" <| fun _ ->
      let subject = true
      Expect.isTrue subject "I compute, therefore I am."


    testCase "I'm skipped (should skip)" <| fun _ ->
      Tests.skiptest "Yup, waiting for a sunny day..."

    testCase "contains things" <| fun _ ->
      Expect.containsAll [| 2; 3; 4 |] [| 2; 4 |]
                         "This is the case; {2,3,4} contains {2,4}"

    testCase "Sometimes I want to ༼ノಠل͟ಠ༽ノ ︵ ┻━┻" <| fun _ ->
      Expect.equal "abcdef" "abcdef" "These should equal"

    testCase "Should return" <| fun _ ->
      Expect.equal (validateNotNegative "" 1) (Ok ()) "These should equal"

    testCase "Test pariticipant for event" <|fun _ -> testGetParticipantsForEvent
  ]