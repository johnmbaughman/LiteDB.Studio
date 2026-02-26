using System;
using System.Collections.Generic;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.Performance;

/// <summary>
/// Memory-leak regression tests: open and discard 100 <see cref="TabViewModel"/>
/// instances, then verify the GC can reclaim them all (no rooted references leaked).
/// </summary>
public class MemoryLeakTests
{
    private const int TabCount = 100;

    // ------------------------------------------------------------------
    // Helpers — object creation MUST live in a separate static method so
    // the JIT does not keep local variable roots alive in the test frame
    // when GC.Collect is called.
    // ------------------------------------------------------------------

    private static WeakReference[] CreateAndReleaseTabViewModels(int count)
    {
        IDatabaseService service = Substitute.For<IDatabaseService>();
        var refs = new WeakReference[count];

        for (var i = 0; i < count; i++)
        {
            // "Open" — create a new tab ViewModel
            var tab = new TabViewModel(service, NullLoggerFactory.Instance) { Title = $"Tab {i}" };
            refs[i] = new WeakReference(tab);
            // "Close" — tab goes out of scope at end of loop iteration
        }

        return refs;
    }

    private static void ForceFullGc()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    // ------------------------------------------------------------------
    // Tests
    // ------------------------------------------------------------------

    [Fact]
    public void Open100Tabs_AfterClose_NoTabViewModelRootsRemain()
    {
        // Arrange + Act — create and immediately release 100 TabViewModels
        WeakReference[] weakRefs = CreateAndReleaseTabViewModels(TabCount);

        // Force full GC so that unreachable objects are collected
        ForceFullGc();

        // Assert — all WeakReferences should be dead (objects reclaimed)
        var stillAlive = new List<int>();
        for (var i = 0; i < weakRefs.Length; i++)
        {
            if (weakRefs[i].IsAlive)
            {
                stillAlive.Add(i);
            }
        }

        Assert.True(
            stillAlive.Count == 0,
            $"{stillAlive.Count}/{TabCount} TabViewModel(s) were not collected by GC " +
            $"(indices: {string.Join(", ", stillAlive)}). Possible rooted reference or event subscription leak.");
    }

    [Fact]
    public void Open100Tabs_AfterClose_MemoryGrowthIsBounded()
    {
        // Establish baseline after a full GC
        ForceFullGc();
        var baselineBytes = GC.GetTotalMemory(true);

        // Open and close 100 tabs
        for (var i = 0; i < TabCount; i++)
        {
            IDatabaseService service = Substitute.For<IDatabaseService>();
            _ = new TabViewModel(service, NullLoggerFactory.Instance) { Title = $"Tab {i}" };
            // Tab goes out of scope; service mock is also released
        }

        // Force GC and measure residual memory
        var finalBytes = GC.GetTotalMemory(true);
        var growthBytes = finalBytes - baselineBytes;

        // Allow up to 10 MB net growth to account for framework/test infrastructure overhead
        const long maxAllowedGrowthBytes = 10 * 1024 * 1024;
        Assert.True(
            growthBytes < maxAllowedGrowthBytes,
            $"Memory grew by {growthBytes / 1024.0:F1} KB after 100 open/close cycles " +
            $"(limit: {maxAllowedGrowthBytes / 1024 / 1024} MB). Possible memory leak.");
    }
}
