# 10. Invoice finalization freezes a specific body of evidence

Read [Invoice](../../src/MeterForge.Domain/Invoicing/Invoice.cs) after the earlier chapters on money, price intervals, and usage identity. An invoice is where calculated amounts become an explicit business document. Its total is important, but the evidence explaining that total is equally important. This chapter derives the current object's contract and distinguishes it from the larger workflow that would be needed to publish, persist, or collect the invoice.

The source defines Draft and Finalized states. A new object defaults to Draft. Construction requires nonempty invoice and subscription identifiers, a nonblank tenant, and an end date after the start date. Currency is normalized through Money. These guards establish a well-formed object; they do not establish that the subscription belongs to the tenant or that this is the only invoice for the billing period.

## Trace construction and the first line

Create an invoice for a synthetic tenant, a synthetic subscription, a start date, and a later end date. At construction its line collection is empty and its total is zero in the invoice currency. Finalization at this point is rejected because at least one line is required. This is a stronger condition than merely having a nonnegative total, but it does not require a positive total.

Add a zero-valued line with a description and source reference. The method accepts a nonnegative amount in the same currency, so this line can satisfy the nonempty condition even though the total remains zero. A learner should not infer that the invoice must be payable merely because it is finalized. The payment model has its own positive-amount guard, which becomes important in chapter 11.

AddLine trims the description and source reference after requiring both to be nonblank. It creates a new line identifier and retains the optional PriceVersionId. A source reference is evidence supplied by the caller; the method does not dereference it or validate that it names an accepted usage operation. The field's presence is a useful boundary, but its truth requires another layer's responsibility.

## Work a monetary trace without hiding rounding

Suppose a caller constructs three line amounts through Money and adds them to a USD invoice. Total folds over the lines from zero using Money addition. The invoice does not store a separately mutable total that can drift from its current line list. This is a useful local invariant: the total is derived from the lines visible in the object.

That invariant does not tell us whether the upstream rating algorithm selected the correct usage or price. An invoice can add correctly and still contain the wrong line. A good test suite separates arithmetic agreement from evidence completeness. One test checks the total of known lines; another checks that the invoicing workflow selected exactly the intended sources.

Use the earlier Money chapter when constructing fractional values. Money requires the supplied amount to already satisfy its two-decimal precision rule; it rejects extra precision rather than silently rounding it. A caller such as the rater may round before constructing Money. The invoice's sum then operates on those accepted monetary values. Comparing "round each line then sum" with "sum raw decimal quantities then round" is a policy question that must be resolved before blaming the invoice aggregate. The trace should record the amount entering AddLine, not only the raw metering quantity.

## Finalization has a narrow meaning

Finalize accepts a timestamp, verifies Draft and nonempty lines, then sets Finalized and normalizes the timestamp to UTC. It does not use the current system clock internally for that decision. This makes time input explicit and permits deterministic tests. The object does not require the supplied instant to be after the billing period or after construction, so a larger workflow must own any such policy.

After finalization, AddLine rejects mutation and a second Finalize rejects the transition. These guards make finalization a one-way operation through the public methods shown. They do not prove the invoice was saved, rendered, emailed, taxed correctly, or charged. Those are separate effects that should have their own evidence and retry contracts.

Consider a workflow that finalizes the in-memory object and then fails while saving a snapshot. The object remains finalized in memory unless the caller discards it or reconstructs state. A retry cannot simply call Finalize again on that same instance because the guard rejects it. This is an ordinary consequence of mutation before persistence, not proof that the domain method is wrong. The application must decide how it handles such an interrupted workflow.

## Read-only collection and immutable line data

Lines exposes an AsReadOnly wrapper over the private list. This differs from returning the List directly under an IReadOnlyCollection type. The wrapper prevents ordinary collection mutation through a downcast to the underlying list type. Its contents still reflect later additions to the aggregate while it remains draft; it is not a detached snapshot.

InvoiceLine is a record with its own values. A caller can create a modified record through a with expression, but doing so does not replace the record stored in the invoice's private collection. This distinction helps learners reason about value copying versus aggregate mutation. A test should confirm that changing a separate record value does not change the aggregate's total.

Do not overstate immutability. The aggregate itself changes while draft, and its collection wrapper is a live view. If a reporting component requires a stable snapshot, it should capture the finalized state under an explicit workflow. A reference to Lines obtained before another AddLine can show the new line afterward. That behavior is compatible with a read-only view and should be tested if consumers rely on it.

## Evidence uniqueness is not automatic

AddLine does not reject a repeated SourceReference. Calling it twice with the same evidence creates two lines and doubles the corresponding amount. That may be legitimate if the reference names a broad calculation batch, or it may reveal a replay problem if the reference is intended to identify one charge. The source alone does not choose the product policy.

A proposed workflow can assign a stable cause identity to each line and enforce uniqueness within an invoice or a billing scope. It must define what makes two lines equivalent: source identity, price version, period segment, quantity, and adjustment category may all matter. Rejecting duplicates by description is unreliable because descriptions are presentation text, not durable business identity.

The invoice identifier also needs a creation policy. A fresh Guid for every retry can create multiple invoices for one intended billing period unless a repository or workflow prevents it. Conversely, one invoice per subscription and period may be too restrictive if the product supports corrective documents. The course asks for an explicit identity rule rather than assuming uniqueness from the presence of an Id property.

## Exercise MF-10A: trace a zero invoice

Construct the state trace for an empty draft, a rejected finalization, an accepted zero line, and successful finalization. Explain why the result can be a valid finalized invoice while failing the payment model's positive-amount requirement. Propose a workflow branch for zero invoices without changing the domain's current behavior.

## Exercise MF-10B: test evidence replay

Add two identical source references to a draft in a disposable domain fixture. Predict line identifiers and total. Then propose a cause-identity policy and a test that distinguishes a true replay from two legitimate charges sharing a human-readable description.

## Exercise MF-10C: design finalization persistence

Place a save failure after in-memory finalization. Specify what a retry reads or reconstructs, how it avoids a second invoice for the same operation, and what timestamp it preserves. Include an explicit limitation if the prototype uses only in-memory storage.

## Review checkpoint

An invoice review should explain both the arithmetic and the provenance. Finalized means the public aggregate methods prevent further line changes; it does not mean every downstream effect completed. The strongest answers use stable operation identity and persisted evidence to bridge that boundary without weakening the local finalization guard.
