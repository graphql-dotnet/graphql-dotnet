# GraphQL.NET Bounty Program

The GraphQL.NET team has worked to create a program where community developers can work on specific new features
of GraphQL.NET for compensation. These features are determined by GraphQL.NET maintainers to have the
potential for high-impact improvement to the product. Design proposals that would fulfill a bounty may
be made either by individuals or teams of developers, but the final payout will be made to only one stakeholder.


## Bounty Process Flowchart

1. The GraphQL.NET team publishes a Request For Proposals (RFP) describing a feature that the team has decided
   should be prioritized, or a bug that is in need of being researched and fixed.
    * Initial Bounty Status set to **Accepting Proposals**
2. Developers write proposals for how they would implement the feature or fix the bug described in the RFP.
3. The GraphQL.NET team works with developers to clarify and refine their proposals.
4. A proposal is accepted. The developer(s) responsible for the proposal work to implement the feature or
   bug fix according to their proposal.
    * Bounty status changed to **In Progress**
5. Pull requests are submitted to implement the proposal by the developers.
    * Bounty status changed to **Under Review**
6. Pull requests are reviewed by project maintainers, and the pull request developers make any changes to the
   pull requests accordingly.
8. Pull requests are merged into the master or develop branch.
9. Developer(s) submit invoice to the [GraphQL.NET Open Collective](https://opencollective.com/graphql-net)
   for bounty amount, confirmed by GraphQL.NET team members.
    * Bounty status changed to **Claimed**
10. Payment disbursed to developer(s) by the Open Source Collective from GraphQL.NET funds.


## How to Claim a Bounty

We've created a process to ensure multiple contributors aren’t competing on the same project, and to make sure
work is properly merged into GraphQL.NET once completed.

1. Read the Request For Proposal document completely.
2. If you want to move forward, submit an proposal to the GraphQL.NET team on your suggested implementation by
   replying to the RFP posting. Be sure to include as many details as requested by the spec, and be sure to address
   each of the completion criteria. For proposals that include UI changes, mockups are strongly encouraged.
3. Work with the GraphQL.NET team to finalize plans for the best implementation. Proposals will be judged on their
   maintainability, design quality, and adherence to spec. Note that other developers may submit competing proposals
   at this time as well.
5. If your proposal is accepted, you may begin code implementation that adheres to your proposal.
7. Submit code as a draft pull request in the appropriate repository so GraphQL.NET community leaders can track progress.
8. Once code is complete, remove draft status and notify GraphQL.NET maintainers.
9. Update code with feedback from core GraphQL.NET contributors as needed.
10. Wait for your code to be merged into the `master` or `develop` branch.
11. [Submit an expense through Open Collective](https://opencollective.com/graphql-net/expenses/new) for the amount of the bounty.


## Collaboration with GraphQL.NET team

Collaboration with the GraphQL.NET team is crucial to having your code pulled into GraphQL.NET. During the proposal process,
clarify any questions you or your team may have as early as possible. During development, use the pull request commenting
feature.


## Deadlines

If your team does not show code commits or interaction for two weeks at a time as part of an accepted feature bounty, then
the bounty will be released for another team to work on. If the task remains in the **Accepting Proposals** state, and the
team can demonstrate work on the feature, they can reclaim the task.

If you no longer wish to work on a project that has been granted to you, please notify the GraphQL.NET team to reset the
bounty as **Accepting Proposals**.


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

The GraphQL.NET team reserves the right to terminate or make changes to this program at any time, including but not limited
to terminating In Process or Under Review bounty projects, or changing their valuation.

By posting any issue, pull request, or other form of submission to GraphQL.NET (a Submission), whether or not it is accepted
through this bounty program, you:

* License the Submission under the terms of the [MIT License](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/LICENSE.md)
   and do not require attribution for your submission;

* understand and acknowledge that others may have developed or commissioned materials similar or identical to your Submission,
   and you waive any claims you may have resulting from any similarities to your Submission;

* understand that you are not guaranteed any compensation or credit for your Submission; and

* represent and warrant that your Submission is your own work, that you haven't used information owned by another person or entity,
  and that you have the legal right to provide the Submission to GraphQL.NET.
