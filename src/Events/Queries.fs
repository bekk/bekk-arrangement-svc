namespace ArrangementService.Events

open Models
open System.Linq

module Queries =

    let queryEventBy id (events: IQueryable<DbModel>) =
        query {
            for event in events do
                where (event.Id = id)
                select (Some event)
                exactlyOneOrDefault
        } //   let queryEventsForEmployee (employeeId: int) (events: IQueryable<DbModel>) =
//       query {
//           for event in events do
//              where (event.ResponsibleEmployee = employeeId)
//             select event
//    }
