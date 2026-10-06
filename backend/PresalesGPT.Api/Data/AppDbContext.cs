using Microsoft.EntityFrameworkCore;
using PresalesGPT.Api.Models;

namespace PresalesGPT.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Opportunity> Opportunities =>
        Set<Opportunity>();

    public DbSet<Document> Documents =>
        Set<Document>();

    public DbSet<Requirement> Requirements =>
        Set<Requirement>();

    public DbSet<ProposalSection> ProposalSections =>
        Set<ProposalSection>();

    public DbSet<SowSection> SowSections =>
        Set<SowSection>();

    public DbSet<DeckSlide> DeckSlides =>
        Set<DeckSlide>();

    public DbSet<Clarification> Clarifications =>
        Set<Clarification>();
}
