﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Jobsway2goMvc.Data;
using Jobsway2goMvc.Models;
using Jobsway2goMvc.Validators.Job_Category;
using Jobsway2goMvc.Validators.Jobs;
using FluentValidation.Results;
using Jobsway2goMvc.Models.ViewModel;
using System.Security.Claims;

namespace Jobsway2goMvc.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Jobs.Include(j => j.Category).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Jobs == null)
            {
                return NotFound();
            }
            var job = await _context.Jobs
                .Include(j => j.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        public IActionResult Create()
        {
            var categories = _context.JobCategories.ToList();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CompanyName,Location,Schedule,Description,OpenSpots,Requirements,DateFrom,DateTo,MinSalary,MaxSalary,CategoryId")] Job job)
        {
            var validator = new JobValidator();
            ValidationResult result = validator.Validate(job);
            
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.ErrorMessage);
                }
                
                var categories = _context.JobCategories.ToList();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");

                return View(job);
            }

            _context.Add(job);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Jobs == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }
            var categories = _context.JobCategories.ToList();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyName,Location,Schedule,Description,OpenSpots,Requirements,DateFrom,DateTo,MinSalary,MaxSalary,CategoryId")] Job job)
        {
            if (id != job.Id)
            {
                return NotFound();
            }
            ModelState.Remove("Category");

            var validator = new JobValidator();
            ValidationResult result = validator.Validate(job);

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.ErrorMessage);
                }
                return View(job);
            }

            try
            {
                _context.Update(job);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobExists(job.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Jobs == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs
                 .Include(j => j.Category)
                 .FirstOrDefaultAsync(m => m.Id == id);
            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Jobs == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Jobs'  is null.");
            }
            var job = await _context.Jobs.FindAsync(id);
            if (job != null)
            {
                _context.Jobs.Remove(job);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SaveToCollection(int? id)
        {
            if (id == null || _context.Jobs == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            JobCollectionViewModel jobCollection = new JobCollectionViewModel();

             jobCollection.Job = job;

            var collections = _context.Collections.Where(a => a.User.Id == HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value).ToList();
            ViewBag.Collections = new SelectList(collections, "Id", "Name");
            return View(jobCollection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveToCollection(Job job, Collection collection)
        {
            JobCollectionViewModel jobCollection = new JobCollectionViewModel
            {
                Job = job,
                Collection = collection
            };

            if (job.Id != 0 || collection.Id != 0)
            {
                if (collection != null && job != null)
                {
                    if (!collection.Jobs.Contains(job))
                    {
                        collection.Jobs.Add(job);
                        _context.Entry(collection).Collection(c => c.Jobs).IsModified = true;
                        await _context.SaveChangesAsync();
                        return RedirectToAction("Index", "Collections");
                    }
                }
                return RedirectToAction("Index", "Jobs");
            }
            return NotFound();
        }

        private bool JobExists(int id)
        {
          return _context.Jobs.Any(e => e.Id == id);
        }
    }
}
