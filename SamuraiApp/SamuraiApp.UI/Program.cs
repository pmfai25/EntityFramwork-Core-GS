﻿using SamuraiApp.Domain;
using SamuraiApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SamuraiApp.UI
{
    class Program
    {
        private static SamuraiContext _context = new SamuraiContext();

        static void Main(string[] args)
        {
            _context.Database.EnsureCreated();
            _context.GetService<ILoggerFactory>().AddProvider(new MyLoggerProvider());
            StartTrackingUsingCustomFunction();            
            Console.ReadLine();
        }

        #region RelatedData
        private static void InsertNewPkFkGraph()
        {
            var samurai = new Samurai
            {
                Name = "Saitama",
                Quotes = new List<Quote>() { new Quote() { Text = "U maiba shinderu"} }
            };
            _context.Samurais.Add(samurai);
            _context.SaveChanges();
        }
        private static void InsertNewOneToOneGraph()
        {
            var samurai = new Samurai
            {
                Name = "Endevor",
                SecretIdentity = new SecretIdentity() { RealName = "Yagamy" }
            };
            _context.Samurais.Add(samurai);
            _context.SaveChanges();
        }
        private static void AddChildToExistingObject()
        {
            var samurai = _context.Samurais.First();            
            _context.Samurais.Include(s => s.Quotes).FirstOrDefault().Quotes.Add(new Quote { Text = "Gomu gomu nooo Jet Pistol" });            
            _context.SaveChanges();
        }
        private static void AddOneToOneToExistingObjectWhileTracked()
        {
            var samurai = _context.Samurais.FirstOrDefault(s => s.SecretIdentity == null);
            samurai.SecretIdentity = new SecretIdentity() { RealName = "Yagamy 1" };
            _context.SaveChanges();
        }                       

        private static void AddBattles()
        {
            _context.Battles.AddRange(                 
                    new Battle() {Name = "Battle of Shiroyama", StartDate = new DateTime(1877, 9, 24), EndDate = new DateTime(1877, 9, 24)},
                    new Battle() {Name = "Siegue of Osaka", StartDate = new DateTime(1614, 1, 1), EndDate = new DateTime(1615, 12, 30)},
                    new Battle() {Name = "Boshin War", StartDate = new DateTime(1868, 1, 1), EndDate = new DateTime(1869, 1, 1)}               
                );
            _context.SaveChanges();
        }
        private static void AddManyToManyWithFKs()
        {
            _context.SamuraiBattles.Add(
                new SamuraiBattle()
                {
                    SamuraiId = 1,
                    BattleId = 1
                }
                );
            _context.SaveChanges();
        }
        private static void AddManyToManyWithObject()
        {
            var samurai = _context.Samurais.FirstOrDefault();
            var battle = _context.Battles.FirstOrDefault();

            _context.SamuraiBattles.Add(
                new SamuraiBattle() { Samurai = samurai, Battle = battle}
                );
            _context.SaveChanges();
        }
        #endregion

        #region  EagerLoadInclude
        private static void EagerLoadWithInclude()
        {
            using (var context = new SamuraiContext())
            {
                var samuraiWithQuotes = context.Samurais.Include(s => s.Quotes).ToList();
            }
        }
        private static void EagerLoadManyToManyAkaChildrenGrandChildren()
        {
            using (var context = new SamuraiContext())
            {
                var samuraiWithBattles = context.Samurais.Include(s => s.SamuraiBattles).ThenInclude(sb => sb.Battle).ToList();
            }
        }
        private static void EagerLoadingWithMultipleBranches()
        {
            using (var context = new SamuraiContext())
            {
                var samuraiWithQuotesAndSecretIdentity = context.Samurais.Include(s => s.Quotes).Include(s => s.SecretIdentity).ToList();
            }
        }
        private static void EagerLoadWithFromSql()
        {
            using (var context = new SamuraiContext())
            {
                var samuraisWithQuotes = context.Samurais.FromSql("SELECT * FROM Samurais").Include(s => s.Quotes).ToList();
            }
        }
        #endregion

        #region EagerLoadProjection
        private static void AnonymousTypeViaProjection()
        {
            using (var context = new SamuraiContext())
            {
                var quotes = context.Quotes
                    .Select(q => new { q.Id, q.Text })
                    .ToList();
            }
        }
        private static void AnonymousTypeViaProjectionWithrelated()
        {
            using (var context = new SamuraiContext())
            {
                var samurais = context.Samurais
                    .Select(s => new {
                        s.Id,
                        s.SecretIdentity.RealName,
                        QuoteCount = s.Quotes.Count
                    })
                    .ToList();
            }
        }
        private static void RelatedObjectsFixUp()
        {
            using (var context = new SamuraiContext())
            {
                var samurai = context.Samurais.Find(1);
                var quotes = context.Quotes.Where(q => q.SamuraiId == 1).ToList();
            }
        }
        private static void EagerLoadViaProjectionNotQuite()
        {
            using (var context = new SamuraiContext())
            {
                var samurais = context.Samurais.Select(s => new {
                    Samurai = s,
                    Quotes = s.Quotes
                })
                .ToList();
            }                
        }
        private static void FilteredEagerLoadViaProjectionNope()
        {
            using (var context = new SamuraiContext())
            {
                var samurais = context.Samurais.Select(s => new {
                    Samurai = s,
                    Quotes = s.Quotes.Where(q => q.Text.Contains("nope")).ToList()
                })
                .ToList();
            }
        }
        #endregion

        #region ExplicitLoad
        private static void ExplicitLoad()
        {
            using (var context = new SamuraiContext())
            {
                var samurai = context.Samurais.FirstOrDefault();
                context.Entry(samurai).Collection(s => s.Quotes).Load();
                context.Entry(samurai).Reference(s => s.SecretIdentity).Load();
            }
        }
        private static void ExplicitLoadWithChildFilter()
        {
            using (var context = new SamuraiContext())
            {
                var samurai = context.Samurais.FirstOrDefault();
                context.Entry(samurai).Collection(s => s.Quotes)
                    .Query()
                    .Where(q => q.Text.Contains("nope"))
                    .Load();
            }
        }
        #endregion

        #region RelatedDataForFiltersAndMore
        private static void UsingRealtedDataForFiltersAndMore()
        {
            using (var context = new SamuraiContext())
            {
                var samurais = context.Samurais.Where(s => s.Quotes.Any(q => q.Text.Contains("happy"))).ToList();
            }
        }
        #endregion

        #region DisconnectedGraphBehaviors
        private static void DisplayState(List<EntityEntry> entities, string method)
        {
            Console.WriteLine(method);
            entities.ForEach(e => Console.WriteLine($"{e.Entity.GetType().Name} : {e.State.ToString()}"));
            Console.WriteLine();
        }

        private static void AddGraphAllNew()
        {
            var samurai = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samurai.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Samurais.Add(samurai);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AddGraphAllNew");
            }
        }
        private static void AddGraphWithKeyValues()
        {
            var samurai = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samurai.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Samurais.Add(samurai);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AddGraphWithKeyValues");
            }
        }
        private static void AttachGraphAllNew()
        {
            var samurai = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samurai.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Samurais.Attach(samurai);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AttachGraphAllNew");
            }
        }
        private static void AttachGraphWithKeyValues()
        {
            var samurai = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samurai.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Samurais.Attach(samurai);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AttachGraphWithKeyValues");
            }
        }
        private static void UpdateGraphAllNew()
        {
            var samurai = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samurai.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Samurais.Update(samurai);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "UpdateGraphAllNew");
            }
        }
        private static void UpdateGraphWithKeyValues()
        {
            var samurai = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samurai.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Samurais.Update(samurai);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "UpdateGraphWithKeyValues");
            }
        }
        private static void DeleteGraphAllNew()
        {
            var samurai = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samurai.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Samurais.Remove(samurai);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "DeleteGraphAllNew");
            }
        }
        private static void DeleteGraphWithKeyValues()
        {
            var samurai = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samurai.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Samurais.Remove(samurai);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "DeleteGraphWithKeyValues");
            }
        }
        #endregion

        #region ChangeAdditionsToChangeTracker
        private static void AddGraphViaEntityStateAllNew()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Entry(samuraiGraph).State = EntityState.Added;
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AddGraphViaEnryStateAllNew");
            }
        }
        private static void AddGraphViaEntityStateWithKeyValues()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Entry(samuraiGraph).State = EntityState.Added;
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AddGraphViaentityStateWithKeyValues");
            }
        }
        private static void AttachGraphViaEntityStateAllNew()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Entry(samuraiGraph).State = EntityState.Unchanged;
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AttachGraphViaEntityStateAllNew");
            }
        }
        private static void AttachGraphViaEntityStateWithKeyValues()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Entry(samuraiGraph).State = EntityState.Unchanged;
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AttachGraphViaEntityStateWithKeyValues");
            }
        }
        private static void UpdateGraphViaEntityStateAllNew()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Entry(samuraiGraph).State = EntityState.Modified;
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "UpdateGraphViaEntityStateAllNew");
            }
        }
        private static void UpdateGraphViaEntityStateWithKeyValues()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Entry(samuraiGraph).State = EntityState.Modified;
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "UpdateGraphViaEntityStateWithKeyValues");
            }
        }
        private static void DeleteGraphViaEntityStateAllNew()
        {
            var samuraiGrap = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samuraiGrap.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Entry(samuraiGrap).State = EntityState.Deleted;
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "DeleteGraphViaEntityStateAllNew");
            }
        }
        private static void DeleteGraphViaEntityStateWithKeyValues()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.Entry(samuraiGraph).State = EntityState.Deleted;
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "DeleteGraphViaEntityStateWithKeyValues");
            }
        }

        private static void ChangeStateUsingEntityState()
        {
            var samuraiGraph = new Samurai() { Name = "He who change state", Id = 1 };
            using (var context = new SamuraiContext())
            {
                context.Entry(samuraiGraph).State = EntityState.Modified;
                Console.WriteLine("State changed using Entity State");
                DisplayState(context.ChangeTracker.Entries().ToList(), "InitialState");

                context.Entry(samuraiGraph).State = EntityState.Added;
                DisplayState(context.ChangeTracker.Entries().ToList(), "New State");
                context.SaveChanges();
            }
        }
        #endregion

        #region ChangingStateViaTrackGraph
        private static void AddGraphViaTrackGraphAllNew()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.ChangeTracker.TrackGraph(samuraiGraph, e => e.Entry.State = EntityState.Added);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AddGraphViaTrackGraphAllNew");
            }
        }
        private static void AddGraphViaTrackGraphWithKeyValues()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.ChangeTracker.TrackGraph(samuraiGraph, e => e.Entry.State = EntityState.Added);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AddGraphViaTrackGraphWithKeyValues");
            }
        }
        private static void AttachGraphViaTrackGraphAllNew()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.ChangeTracker.TrackGraph(samuraiGraph, e => e.Entry.State = EntityState.Unchanged);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AttachGraphViaTrackGraphAllNew");
            }
        }
        private static void AttachGraphViaTrackGraphWithKeyValues()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.ChangeTracker.TrackGraph(samuraiGraph, e => e.Entry.State = EntityState.Unchanged);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "AttachGraphViaTrackGraphWithKeyValues");
            }
        }
        private static void UpdateGraphViaTrackGraphAllNew()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.ChangeTracker.TrackGraph(samuraiGraph, e => e.Entry.State = EntityState.Modified);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "UpdateGraphViaTrackGraphAllNew");
            }
        }
        private static void UpdateGraphViaTrackhGrapWithKeyValues()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.ChangeTracker.TrackGraph(samuraiGraph, e => e.Entry.State = EntityState.Modified);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "UpdateGraphViaTrackhGrapWithKeyValues");
            }
        }
        private static void DeleteGraphViaTrackGraphAllNew()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo" };
            var quote = new Quote() { Text = "This is New" };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.ChangeTracker.TrackGraph(samuraiGraph, e => e.Entry.State = EntityState.Deleted);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "DeleteGraphViaTrackGraphAllNew");
            }
        }
        private static void DeleteGraphViaTrackGraphWithKeyValues()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New", Id = 1 };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.ChangeTracker.TrackGraph(samuraiGraph, e => e.Entry.State = EntityState.Deleted);
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "DeleteGraphViaTrackGraphWithKeyValues");
            }
        }

        private static void StartTrackingUsingCustomFunction()
        {
            var samuraiGraph = new Samurai() { Name = "Gingo", Id = 1 };
            var quote = new Quote() { Text = "This is New" };
            samuraiGraph.Quotes.Add(quote);
            using (var context = new SamuraiContext())
            {
                context.ChangeTracker.TrackGraph(samuraiGraph, e => ApplyStateUsingKeySet(e.Entry));
                var entries = context.ChangeTracker.Entries().ToList();
                DisplayState(entries, "StartTrackingUsingCustomFunction");
            }
        }
        private static void ApplyStateUsingKeySet(EntityEntry entity)
        {
            if(entity.IsKeySet)
            {
                entity.State = EntityState.Unchanged;
            }
            else
            {
                entity.State = EntityState.Added;
            }
        }
        #endregion
    }
}
