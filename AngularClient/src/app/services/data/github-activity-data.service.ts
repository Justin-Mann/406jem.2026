import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, from, map, Observable, of, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GitHubActivitySettings, GitHubRepo } from '../../interfaces/github-activity.interface';

/**
 * Backs the Projects page's GitHub Activity card (#68). Reads the configured owner's display
 * settings from #69's public endpoint, then fetches that owner's repos directly from GitHub's
 * public, unauthenticated REST API via the native fetch() - deliberately NOT through HttpClient,
 * since the app-wide authInterceptor unconditionally sets withCredentials on every request, and
 * a credentialed request against GitHub's wildcard-origin CORS response is rejected outright by
 * the browser.
 */
@Injectable({
  providedIn: 'root'
})
export class GitHubActivityDataService {
  private http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  /** Emits null when the feature is off, unconfigured, or either fetch fails - callers should
   * render nothing (not a broken/empty card) in every null case. */
  getActivity(): Observable<GitHubRepo[] | null> {
    return this.http.get<GitHubActivitySettings>(`${this.apiBaseUrl}/api/github-activity-settings/public`).pipe(
      switchMap(settings => {
        if (!settings?.enabled || !settings.gitHubUsername) {
          return of(null);
        }

        return this.getReposForUsername(settings.gitHubUsername).pipe(
          map(repos => repos ? selectRepos(repos, settings.pinnedRepoNames, settings.repoCount) : null)
        );
      }),
      catchError(() => of(null))
    );
  }

  /** Also used directly by the admin settings page (#68 follow-up) to populate its "pin from
   * your repos" picker - emits null on any failure, same as the display path. */
  getReposForUsername(username: string): Observable<GitHubRepo[] | null> {
    const url = `https://api.github.com/users/${encodeURIComponent(username)}/repos?sort=pushed&per_page=100&type=owner`;
    return from(
      fetch(url, { headers: { Accept: 'application/vnd.github+json' } }).then(response => {
        if (!response.ok) {
          throw new Error(`GitHub API responded ${response.status}`);
        }
        return response.json() as Promise<GitHubRepo[]>;
      })
    ).pipe(catchError(() => of(null)));
  }
}

/** Pinned repos first (in pinned order, skipping names that don't match any fetched repo), then
 * the remaining non-fork repos by most-recently-pushed, up to repoCount total - pinned repos
 * count toward that total, they don't add to it. */
export function selectRepos(repos: GitHubRepo[], pinnedRepoNames: string[], repoCount: number): GitHubRepo[] {
  const nonForks = repos.filter(r => !r.fork);
  const byName = new Map(nonForks.map(r => [r.name.toLowerCase(), r]));

  const used = new Set<string>();
  const result: GitHubRepo[] = [];

  for (const pinnedName of pinnedRepoNames) {
    if (result.length >= repoCount) break;

    const repo = byName.get(pinnedName.toLowerCase());
    if (repo && !used.has(repo.name)) {
      result.push(repo);
      used.add(repo.name);
    }
  }

  const remaining = nonForks
    .filter(r => !used.has(r.name))
    .sort((a, b) => new Date(b.pushed_at).getTime() - new Date(a.pushed_at).getTime());

  for (const repo of remaining) {
    if (result.length >= repoCount) break;
    result.push(repo);
    used.add(repo.name);
  }

  return result;
}
