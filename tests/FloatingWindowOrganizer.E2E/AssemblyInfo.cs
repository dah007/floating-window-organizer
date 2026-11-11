// E2E tests drive the real mouse and keyboard, which are shared by the whole desktop, and every
// launched widget opens at the same default position. Run test classes one at a time so one
// test's clicks never land on another test's widget or context menu.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
