namespace FsAssay.Specimens.SectionE

open System

module ErrorsAndResourceHandling =
    // FSA2021 / FSA1006 — Generic Catch
    let handleOperation () =
        try
            10 / 0
        with
        // EXPECT: FSA1006
        | :? System.Exception as e -> -1

    // FSA2022 / FSA2029 — Exception Throwing in Domain
    let validateAge age =
        if age < 0 then
            // EXPECT: FSA2029
            failwith "Age cannot be negative"
        else age

    // FSA2024 — Statement-Style Branching
    let getStatus (active: bool) =
        // EXPECT: FSA1001
        let mutable status = "Inactive"
        if active then
            status <- "Active"
        status
