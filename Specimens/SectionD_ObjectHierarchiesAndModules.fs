namespace FsAssay.Specimens.SectionD

open System

// FSA2018 — Inheritance Depth
// EXPECT: FSA1008
type BaseEntity() =
    abstract member Id : Guid with get, set

// EXPECT: FSA1008
// EXPECT: FSA2018
type CustomerEntity() =
    inherit BaseEntity()
    override this.ToString() = "Customer"
