namespace FsAssay.Specimens.SectionF

open System

module DomainModeling =
    // FSA2027 / FSA1004 — Primitive Obsession
    // EXPECT: FSA1004
    type EmailAddress = string

    // FSA2030 — Boolean Flag Parameters
    let processOrder (isPriority: bool) (sendReceipt: bool) (expressDelivery: bool) = ()
