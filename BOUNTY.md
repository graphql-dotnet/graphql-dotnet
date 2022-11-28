# GraphQL.NET Bounty Program

The GraphQL.NET team has worked to create a program where community developers can work on specific new features
of GraphQL.NET for compensation ("projects"). These projects are determined by GraphQL.NET maintainers to have the
potential for high-impact improvement to the product. Design proposals that would fulfill a bounty may
be made either by individuals or teams of developers, but the final payout will be made to only one stakeholder.
Routine bug fixes and similar pull requests do not fall into this program at this time.


## Bounty Process Flowchart

1. The GraphQL.NET team publishes a Request For Proposals (RFP) describing a feature that the team has decided
   should be prioritized, or a bug that is in need of being researched and fixed (a "project"). This project
   will be posted as a GitHub issue and will indicate the amount of the bounty in US dollars (USD).
     * The GitHub issue is labeled with **`bounty`**.
2. Developers write proposals for how they would implement the feature or fix the bug described in the RFP.
3. The GraphQL.NET team works with developers to clarify and refine their proposals.
4. A proposal is accepted. The developer(s) responsible for the proposal work to implement the feature or
   bug fix according to their proposal.
    * The GitHub issue is also labeled with **`in progress`**.
5. Pull requests are submitted to implement the proposal by the developer(s).
    * The GitHub pull request is linked to the issue.
6. Pull requests are reviewed by project maintainers, and the pull request developers make any changes to the
   pull requests accordingly.
7. Pull requests are merged into the appropriate branch (typically `master` or `develop`).
8. Developer(s) submit invoice to the [GraphQL.NET Open Collective](https://opencollective.com/graphql-net)
   for bounty amount, confirmed by GraphQL.NET team members.
9. Payment disbursed to developer(s) by the Open Source Collective from GraphQL.NET funds;
   i.e. Open Collective is responsible for the actual payment.
    * The GitHub issue is labeled with **`bounty-paid`** and the aforementioned **`bounty`** and **`in progress`** labels are removed.


## How to Claim a Bounty

We've created a process to ensure multiple contributors aren’t competing on the same project, and to make sure
work is properly merged into GraphQL.NET once completed.

1. Read the GitHub issue containing the project's Request For Proposals completely.
2. If you want to move forward, submit an proposal to the GraphQL.NET team on your suggested implementation by
   replying to the issue. Be sure to include as many details as requested by the spec, and be sure to address
   each of the completion criteria. For proposals that include UI changes, mockups are strongly encouraged.
3. Work with the GraphQL.NET team to finalize plans for the best implementation. Proposals will be judged on their
   maintainability, design quality, and adherence to spec. Note that other developers may submit competing proposals
   at this time as well.
4. If your proposal is accepted, you may begin code implementation that adheres to your proposal.
5. Submit code as a draft pull request in the appropriate repository so GraphQL.NET community leaders can track progress.
6. Once code is complete, remove draft status and request a review from the GraphQL.NET maintainers.
7. Update code with feedback from core GraphQL.NET contributors as needed.
8. Wait for your code to be merged into the `master` or `develop` branch.
9. [Submit an expense through Open Collective](https://opencollective.com/graphql-net/expenses/new) for the amount of the bounty.


## Collaboration with GraphQL.NET team

Collaboration with the GraphQL.NET team is crucial to having your code pulled into GraphQL.NET. During the proposal process,
clarify any questions you or your team may have as early as possible. During development, use the pull request commenting
feature.


## Deadlines

If your team does not show code commits or interaction for two weeks at a time as part of an **`in progress`** bounty, then
the bounty will be released for another team to work on. If the team can demonstrate work on the project, they can reclaim
the bounty.

If you no longer wish to work on a bounty that has been granted to you, please notify the GraphQL.NET team.


## Bounty Valuation

The GraphQL.NET team currently uses a simple rubric to determine how to price a bounty. Generally speaking, RFPs are evaluated
based on two main criteria:

* How complex is the feature/bugfix?
    * How long do we expect it may take to complete?
    * How much specialized knowledge do we expect it will require to implement?
* How high of a demand is there for this feature/bugfix?
    * How many people do we expect this to impact?
    * How frequently are we asked about this feature or bug?
    * How urgently does this need to be implemented?

In general, issues that are higher in complexity and higher in demand are given greater bounties.


## Legal

You ARE eligible to participate in the Program if you meet all of the following criteria:

* You are 14 years of age or older. If you are at least 14 years old but are considered a minor in your place of residence,
  you must obtain your parent's or legal guardian's permission prior to participating in this Program; and

* You are either an individual researcher participating in your own individual capacity, or you work for an organization
  that permits you to participate. You are responsible for reviewing your employer's rules for participating in this Program.

You ARE NOT eligible to participate in the Program if you meet any of the following criteria:

* You are a resident of any countries under U.S. sanctions (see link for current sanctions list posted by the United States
  Treasury Department) or any other country that does not allow participation in this type of program; or

* You are under the age of 14.

By posting any issue, pull request, or other form of submission to GraphQL.NET (a Submission), whether or not it is accepted
through this bounty program, you:

* License the Submission under the terms of the [MIT License](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/LICENSE.md)
  and do not require attribution for your submission;

* Understand and acknowledge that others may have developed or commissioned materials similar or identical to your Submission,
  and you waive any claims you may have resulting from any similarities to your Submission;

* Understand that you are not guaranteed any compensation or credit for your Submission; and

* Represent and warrant that your Submission is your own work, that you haven't used information owned by another person or entity,
  and that you have the legal right to provide the Submission to GraphQL.NET.

The GraphQL.NET team reserves the right to terminate or make changes to this program at any time, including but not limited
to terminating **`in progress`** bounty projects, or changing their valuation.
