import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { GitHubActivitySettingsDataService } from '../services/data/github-activity-settings-data.service';
import { GitHubActivityDataService } from '../services/data/github-activity-data.service';
import { AuthService } from '../services/auth/auth.service';
import { emptyGitHubActivitySettings, GitHubActivitySettingsDto } from '../interfaces/github-activity-settings.interface';
import { GitHubRepo } from '../interfaces/github-activity.interface';

@Component({
  selector: 'app-github-activity-settings',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
  ],
  templateUrl: './github-activity-settings.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './github-activity-settings.component.css',
})
export class GitHubActivitySettingsComponent implements OnInit {
  private dataService = inject(GitHubActivitySettingsDataService);
  private gitHubActivityDataService = inject(GitHubActivityDataService);
  authService = inject(AuthService);

  settings = signal<GitHubActivitySettingsDto | null>(null);
  newPinnedRepo = signal('');
  errorMessage = signal<string | null>(null);
  statusMessage = signal<string | null>(null);
  isBusy = signal(false);
  availableRepos = signal<GitHubRepo[]>([]);
  reposLoading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.errorMessage.set(null);
    this.dataService.getMine().subscribe({
      next: settings => {
        this.settings.set(settings);
        this.loadAvailableRepos();
      },
      error: () => {
        this.errorMessage.set('Could not load your GitHub Activity settings.');
        this.settings.set(emptyGitHubActivitySettings());
      }
    });
  }

  loadAvailableRepos(): void {
    const username = this.settings()?.gitHubUsername;
    if (!username) {
      this.availableRepos.set([]);
      return;
    }

    this.reposLoading.set(true);
    this.gitHubActivityDataService.getReposForUsername(username).subscribe(repos => {
      this.reposLoading.set(false);
      // Includes forks - unlike the display page's automatic fill-by-recency, pinning here is
      // a deliberate admin choice, and selectRepos now honors a pinned fork regardless.
      this.availableRepos.set(repos ?? []);
    });
  }

  addPinnedRepo(): void {
    const name = this.newPinnedRepo().trim();
    const settings = this.settings();
    if (!name || !settings) return;

    this.settings.set({ ...settings, pinnedRepoNames: [...settings.pinnedRepoNames, name] });
    this.newPinnedRepo.set('');
  }

  removePinnedRepo(index: number): void {
    const settings = this.settings();
    if (!settings) return;

    this.settings.set({ ...settings, pinnedRepoNames: settings.pinnedRepoNames.filter((_, i) => i !== index) });
  }

  movePinnedRepoUp(index: number): void {
    const settings = this.settings();
    if (!settings || index <= 0) return;

    const names = [...settings.pinnedRepoNames];
    [names[index - 1], names[index]] = [names[index], names[index - 1]];
    this.settings.set({ ...settings, pinnedRepoNames: names });
  }

  movePinnedRepoDown(index: number): void {
    const settings = this.settings();
    if (!settings || index >= settings.pinnedRepoNames.length - 1) return;

    const names = [...settings.pinnedRepoNames];
    [names[index + 1], names[index]] = [names[index], names[index + 1]];
    this.settings.set({ ...settings, pinnedRepoNames: names });
  }

  onPinnedSelectionChange(selected: string[]): void {
    const settings = this.settings();
    if (!settings) return;

    const stillPinned = settings.pinnedRepoNames.filter(n => selected.includes(n));
    const added = selected.filter(n => !settings.pinnedRepoNames.includes(n));
    this.settings.set({ ...settings, pinnedRepoNames: [...stillPinned, ...added] });
  }

  save(): void {
    const settings = this.settings();
    if (!settings) return;

    this.isBusy.set(true);
    this.errorMessage.set(null);
    this.statusMessage.set(null);

    this.dataService.updateMine({
      enabled: settings.enabled,
      gitHubUsername: settings.gitHubUsername,
      repoCount: settings.repoCount,
      pinnedRepoNames: settings.pinnedRepoNames,
    }).subscribe({
      next: saved => {
        this.isBusy.set(false);
        this.settings.set(saved);
        this.statusMessage.set('GitHub Activity settings saved.');
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not save your GitHub Activity settings. Please try again.');
      }
    });
  }
}
